using Domain.Common.Results;

namespace Domain.Orders;

/// <summary>
/// 注文ドメインに関するエラー定義
/// </summary>
/// <remarks>
/// Result パターンで使用される静的エラーファクトリクラスです。
/// 例外を投げずに失敗を表現するための <see cref="Error"/> インスタンスを生成します。
/// </remarks>
public static class OrderErrors
{
    /// <summary>
    /// 指定された注文IDに対応する注文が見つからない場合のエラーを生成します（HTTP 404）
    /// </summary>
    /// <param name="orderId">見つからなかった注文ID</param>
    /// <returns>NotFound エラー</returns>
    public static Error NotFoundByOrderId(int orderId) => Error.NotFound(
        "Order.NotFound",
        $"Order not found for orderId: {orderId}");

    /// <summary>
    /// 在庫不足の場合のエラーを生成します（HTTP 400）
    /// </summary>
    /// <param name="productId">在庫不足の商品ID</param>
    /// <param name="available">現在の在庫数</param>
    /// <param name="requested">要求された数量</param>
    /// <returns>Problem エラー</returns>
    public static Error InsufficientStock(int productId, int available, int requested) => Error.Problem(
        "Order.InsufficientStock",
        $"ProductId={productId}, Available={available}, Requested={requested}");

    /// <summary>
    /// 注文アイテムが空の場合のエラーを生成します（HTTP 400）
    /// </summary>
    /// <returns>Problem エラー</returns>
    public static Error EmptyOrder() => Error.Problem(
        "Order.EmptyOrder",
        "Order must have at least one item.");
}
