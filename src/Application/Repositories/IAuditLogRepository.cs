using Domain.Entities;

namespace Application.Repositories;

/// <summary>
/// 監査ログリポジトリのインターフェース
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// 監査ログを作成します
    /// </summary>
    /// <param name="log">作成する監査ログ</param>  
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task CreateAsync(
        AuditLog log,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// すべての監査ログを取得します
    /// </summary>
    /// <param name="limit">取得件数の上限（デフォルト: 100）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>監査ログのリスト（新しい順）</returns>
    Task<IEnumerable<AuditLog>> GetAllAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);
}
