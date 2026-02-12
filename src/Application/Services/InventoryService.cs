using Application.Common;
using Application.Repositories;
using Domain.AuditLog;
using Domain.Common.Results;
using Domain.Inventory;

namespace Application.Services;

/// <summary>
/// 在庫サービス
/// </summary>
/// <remarks>
/// 在庫管理のビジネスロジック
/// </remarks>
public class InventoryService(
    IUnitOfWork uow,
    IInventoryRepository inventory,
    IAuditLogRepository auditLog)
{
    /// <summary>
    /// すべての在庫を取得します
    /// </summary>
    public async Task<Result<IEnumerable<Inventory>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var inventories = await inventory.GetAllAsync(cancellationToken);
        return Result.Success(inventories);
    }

    /// <summary>
    /// 商品IDを指定して在庫を取得します
    /// </summary>
    public async Task<Result<Inventory>> GetByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var inventoryEntity = await inventory.GetByProductIdAsync(productId, cancellationToken);
        if (inventoryEntity is null)
        {
            return Result.Failure<Inventory>(InventoryErrors.NotFoundByProductId(productId));
        }
        return Result.Success(inventoryEntity);
    }

    /// <summary>
    /// 在庫を作成します
    /// </summary>
    /// <param name="productName">商品名</param>
    /// <param name="stock">在庫数</param>
    /// <param name="unitPrice">単価</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>作成された商品ID</returns>
    public async Task<Result<int>> CreateAsync(
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var productId = await inventory.CreateAsync(new Inventory
            {
                ProductName = productName,
                Stock = stock,
                UnitPrice = unitPrice
            }, cancellationToken);

            await auditLog.CreateAsync(new AuditLog
            {
                Action = "INVENTORY_CREATED",
                Details = $"ProductId={productId}, Name={productName}, Stock={stock}, Price={unitPrice}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Result.Success(productId);
        }, cancellationToken);
    }

    /// <summary>
    /// 在庫を更新します
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="productName">商品名</param>
    /// <param name="stock">在庫数</param>
    /// <param name="unitPrice">単価</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public async Task<Result> UpdateAsync(
        int productId,
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var existing = await inventory.GetByProductIdAsync(productId, cancellationToken);
            if (existing is null)
            {
                return Result.Failure(InventoryErrors.NotFoundByProductId(productId));
            }

            await inventory.UpdateAsync(productId, productName, stock, unitPrice, cancellationToken);

            await auditLog.CreateAsync(new AuditLog
            {
                Action = "INVENTORY_UPDATED",
                Details = $"ProductId={productId}, Name={productName}, Stock={stock}, Price={unitPrice}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Result.Success();
        }, cancellationToken);
    }

    /// <summary>
    /// 在庫を削除します
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public async Task<Result> DeleteAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var existing = await inventory.GetByProductIdAsync(productId, cancellationToken);
            if (existing is null)
            {
                return Result.Failure(InventoryErrors.NotFoundByProductId(productId));
            }

            await inventory.DeleteAsync(productId, cancellationToken);

            await auditLog.CreateAsync(new AuditLog
            {
                Action = "INVENTORY_DELETED",
                Details = $"ProductId={productId}, Name={existing.ProductName}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Result.Success();
        }, cancellationToken);
    }
}
