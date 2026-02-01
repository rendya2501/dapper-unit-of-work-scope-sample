using Domain.Common.Results;

namespace Application.Common;

/// <summary>
/// トランザクション管理を提供するUnit of Workインターフェース
/// Result型を判定して自動的にCommit/Rollbackを実行
/// </summary>
public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// トランザクション内で操作を実行
    /// 成功時は自動Commit、失敗時は自動Rollback
    /// </summary>
    Task<OperationResult<T>> ExecuteInTransactionAsync<T>(
        Func<Task<OperationResult<T>>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// トランザクション内で操作を実行（戻り値なし版）
    /// </summary>
    Task<OperationResult> ExecuteInTransactionAsync(
        Func<Task<OperationResult>> operation,
        CancellationToken cancellationToken = default);
}
