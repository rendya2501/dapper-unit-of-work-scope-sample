using OrderManagement.Application.Models;
using OrderManagement.Domain.Common.Results;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Services.Abstractions;

/// <summary>
/// 注文サービスのインターフェース
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// 注文を作成します
    /// </summary>
    /// <param name="customerId">顧客ID</param>
    /// <param name="items">注文する商品と数量のリスト</param>
    /// <returns>作成された注文ID</returns>
    Task<OperationResult<int>> CreateOrderAsync(int customerId, List<OrderItem> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// すべての注文を取得します
    /// </summary>
    Task<OperationResult<IEnumerable<Order>>> GetAllOrdersAsync();

    /// <summary>
    /// IDを指定して注文を取得します
    /// </summary>
    Task<OperationResult<Order>> GetOrderByIdAsync(int id);
}
