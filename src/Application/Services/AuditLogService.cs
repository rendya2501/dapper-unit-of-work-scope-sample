using Application.Repositories;
using Domain.AuditLog;
using Domain.Common.Results;

namespace Application.Services;

/// <summary>
/// 監査ログサービス
/// </summary>
public class AuditLogService(IAuditLogRepository repository)
{
    /// <summary>
    /// すべての監査ログを取得します
    /// </summary>
    /// <param name="limit">取得件数の上限（デフォルト: 100）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>監査ログのリスト（新しい順）</returns>
    public async Task<Result<IEnumerable<AuditLog>>> GetAllAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var auditLogs = await repository.GetAllAsync(limit, cancellationToken);
        return Result.Success(auditLogs);
    }
}
