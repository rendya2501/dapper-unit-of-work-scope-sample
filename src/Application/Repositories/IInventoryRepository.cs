using Domain.Entities;

namespace Application.Repositories;

/// <summary>
/// 在庫リポジトリのインターフェース
/// </summary>
public interface IInventoryRepository
{
    /// <summary>
    /// 商品IDを指定して在庫を取得します
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>在庫情報。見つからない場合は null</returns>
    Task<Inventory?> GetByProductIdAsync(
        int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// すべての在庫を取得します
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>在庫のリスト</returns>
    Task<IEnumerable<Inventory>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在庫を新規作成します
    /// </summary>
    /// <param name="inventory">在庫情報</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>作成された行数</returns>
    Task<int> CreateAsync(
        Inventory inventory, CancellationToken cancellationToken = default);

    /// <summary>
    /// 在庫数を更新します
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="newStock">新しい在庫数</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>更新された行数</returns>
    Task<int> UpdateStockAsync(
        int productId, int newStock, CancellationToken cancellationToken = default);

    /// <summary>
    /// 在庫を更新します
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="productName">商品名</param>
    /// <param name="stock">在庫数</param>
    /// <param name="unitPrice">単価</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task UpdateAsync(
        int productId, 
        string productName, 
        int stock, decimal
        unitPrice, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在庫を削除します
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task DeleteAsync(
        int productId, CancellationToken cancellationToken = default);
}
