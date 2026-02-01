using Domain.Common.Results;
using Domain.Entities;

namespace Application.Services.Abstractions;

/// <summary>
/// 在庫サービスのインターフェース
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// すべての在庫を取得します
    /// </summary>
    Task<OperationResult<IEnumerable<Inventory>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 商品IDを指定して在庫を取得します
    /// </summary>
    Task<OperationResult<Inventory>> GetByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在庫を作成します
    /// </summary>
    /// <param name="productName">商品名</param>
    /// <param name="stock">在庫数</param>
    /// <param name="unitPrice">単価</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>作成された商品ID</returns>
    Task<OperationResult<int>> CreateAsync(
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在庫を更新します
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="productName">商品名</param>
    /// <param name="stock">在庫数</param>
    /// <param name="unitPrice">単価</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task<OperationResult> UpdateAsync(
        int productId,
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在庫を削除します
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task<OperationResult> DeleteAsync(
        int productId, 
        CancellationToken cancellationToken = default);
}
