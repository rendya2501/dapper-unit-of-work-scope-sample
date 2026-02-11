using Domain.AuditLog;

namespace Web.Api.Contracts.AuditLogs.Responses;

/// <summary>
/// <see cref="AuditLog"/> ドメインモデルを <see cref="AuditLogResponse"/> に変換する拡張メソッド
/// </summary>
public static class AuditLogMappingExtensions
{
    /// <summary>
    /// <see cref="AuditLog"/> を <see cref="AuditLogResponse"/> に変換します
    /// </summary>
    /// <param name="log">変換対象の監査ログ</param>
    /// <returns>監査ログのレスポンスDTO</returns>
    public static AuditLogResponse ToResponse(this AuditLog log) =>
        new(
            log.Id,
            log.Action,
            log.Details,
            log.CreatedAt);
}
