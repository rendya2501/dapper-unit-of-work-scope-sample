using OrderManagement.Domain.Common.Results;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Services.Abstractions;

/// <summary>
/// 在庫サービスのインターフェース
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// すべての在庫を取得します
    /// </summary>
    Task<OperationResult<IEnumerable<Inventory>>> GetAllAsync();

    /// <summary>
    /// 商品IDを指定して在庫を取得します
    /// </summary>
    Task<OperationResult<Inventory>> GetByProductIdAsync(int productId);

    /// <summary>
    /// 在庫を作成します
    /// </summary>
    Task<OperationResult<int>> CreateAsync(
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在庫を更新します
    /// </summary>
    Task<OperationResult> UpdateAsync(
        int productId,
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在庫を削除します
    /// </summary>
    Task<OperationResult> DeleteAsync(int productId, CancellationToken cancellationToken = default);
}
