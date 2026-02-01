using Application.Models;
using Domain.Common.Results;
using Domain.Entities;

namespace Application.Services.Abstractions;

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
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>作成された注文ID</returns>
    Task<OperationResult<int>> CreateOrderAsync(
        int customerId, 
        List<OrderItem> items, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// すべての注文を取得します
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>注文のリスト</returns>
    Task<OperationResult<IEnumerable<Order>>> GetAllOrdersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// IDを指定して注文を取得します
    /// </summary>
    /// <param name="id">注文ID</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>注文</returns>
    Task<OperationResult<Order>> GetOrderByIdAsync(
        int id,
        CancellationToken cancellationToken = default);
}
