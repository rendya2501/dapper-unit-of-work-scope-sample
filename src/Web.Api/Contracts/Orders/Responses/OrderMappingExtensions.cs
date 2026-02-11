using Domain.Orders;

namespace Web.Api.Contracts.Orders.Responses;

/// <summary>
/// <see cref="Order"/> / <see cref="OrderDetail"/> ドメインモデルをレスポンスDTOに変換する拡張メソッド
/// </summary>
public static class OrderMappingExtensions
{
    /// <summary>
    /// <see cref="Order"/> を <see cref="OrderResponse"/> に変換します
    /// </summary>
    /// <param name="order">変換対象の注文エンティティ（明細を含む）</param>
    /// <returns>注文のレスポンスDTO</returns>
    public static OrderResponse ToResponse(this Order order) =>
        new(
            order.Id,
            order.CustomerId,
            order.CreatedAt,
            order.TotalAmount,
            order.Details.Select(d => d.ToResponse()));

    /// <summary>
    /// <see cref="OrderDetail"/> を <see cref="OrderDetailResponse"/> に変換します
    /// </summary>
    /// <param name="detail">変換対象の注文明細エンティティ</param>
    /// <returns>注文明細のレスポンスDTO</returns>
    public static OrderDetailResponse ToResponse(this OrderDetail detail) =>
        new(
            detail.ProductId,
            detail.Quantity,
            detail.UnitPrice,
            detail.SubTotal);
}
