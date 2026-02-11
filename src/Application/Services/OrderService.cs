using Application.Common;
using Application.Models;
using Application.Repositories;
using Domain.AuditLog;
using Domain.Common.Results;
using Domain.Inventory;
using Domain.Orders;

namespace Application.Services;

/// <summary>
/// 注文サービスの実装
/// </summary>
/// <remarks>
/// <para><strong>ビジネスロジックの実装場所</strong></para>
/// <list type="bullet">
/// <item>在庫確認・減算</item>
/// <item>注文集約の構築</item>
/// <item>トランザクション境界の管理</item>
/// <item>監査ログの記録</item>
/// </list>
/// </remarks>
/// <param name="uow">Unit of Work（DI経由で注入）</param>
public class OrderService(
    IUnitOfWork uow,
    IInventoryRepository inventory,
    IOrderRepository order,
    IAuditLogRepository auditLog)
{
    /// <inheritdoc />
    public async Task<Result<int>> CreateOrderAsync(
        int customerId,
        List<OrderItem> items,
        CancellationToken cancellationToken)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            // 注文アイテムが空の場合はエラー
            if (items.Count == 0)
            {
                return Result.Failure<int>(OrderErrors.EmptyOrder());
            }

            // 1. 注文集約を構築
            var orderEntity = new Order
            {
                CustomerId = customerId,
                CreatedAt = DateTime.UtcNow
            };

            // 2. 各商品の在庫確認と注文明細追加
            foreach (var item in items)
            {
                var productEntity = await inventory.GetByProductIdAsync(item.ProductId);
                if (productEntity is null)
                {
                    return Result.Failure<int>(InventoryErrors.NotFoundByProductId(item.ProductId));
                }

                // 在庫確認 (十分な在庫がない場合はエラー)
                if (productEntity.Stock < item.Quantity)
                {
                    return Result.Failure<int>(
                        OrderErrors.InsufficientStock(item.ProductId, productEntity.Stock, item.Quantity));
                }

                // 在庫減算
                await inventory.UpdateStockAsync(
                    item.ProductId,
                    productEntity.Stock - item.Quantity);

                // 注文明細を追加（集約ルートを通じて）
                orderEntity.AddDetail(item.ProductId, item.Quantity, productEntity.UnitPrice);
            }

            // 3. 注文を永続化（明細も一緒に保存される）
            var orderId = await order.CreateAsync(orderEntity);

            // 4. 監査ログ記録
            await auditLog.CreateAsync(new AuditLog
            {
                Action = "ORDER_CREATED",
                Details = $"OrderId={orderId}, CustomerId={customerId}, " +
                    $"Items={items.Count}, Total={orderEntity.TotalAmount:C}",
                CreatedAt = DateTime.UtcNow
            });

            return Result.Success(orderId);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Order>>> GetAllOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = await order.GetAllAsync(cancellationToken);
        return Result.Success(orders);
    }

    /// <inheritdoc />
    public async Task<Result<Order>> GetOrderByIdAsync(int id,
        CancellationToken cancellationToken = default)
    {
        var orderEntity = await order.GetByIdAsync(id, cancellationToken);
        if (orderEntity is null)
        {
            return Result.Failure<Order>(OrderErrors.NotFoundByOrderId(id));
        }
        return Result.Success(orderEntity);
    }
}
