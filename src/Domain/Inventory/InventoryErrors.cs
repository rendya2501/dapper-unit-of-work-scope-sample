using Domain.Common.Results;

namespace Domain.Inventory;

/// <summary>
/// 在庫ドメインに関するエラー定義
/// </summary>
/// <remarks>
/// Result パターンで使用される静的エラーファクトリクラスです。
/// 例外を投げずに失敗を表現するための <see cref="Error"/> インスタンスを生成します。
/// </remarks>
public static class InventoryErrors
{
    /// <summary>
    /// 指定された商品IDに対応する在庫が見つからない場合のエラーを生成します（HTTP 404）
    /// </summary>
    /// <param name="productId">見つからなかった商品ID</param>
    /// <returns>NotFound エラー</returns>
    public static Error NotFoundByProductId(int productId) => Error.NotFound(
        "Inventory.NotFound",
        $"Inventory not found for productId: {productId}");
}
