using Domain.Common.Results;
using Domain.Entities;

namespace Application.Services.Abstractions;

/// <summary>
/// 監査ログサービスのインターフェース
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// すべての監査ログを取得します
    /// </summary>
    /// <param name="limit">取得件数の上限（デフォルト: 100）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>監査ログのリスト（新しい順）</returns>
    Task<OperationResult<IEnumerable<AuditLog>>> GetAllAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);
}
