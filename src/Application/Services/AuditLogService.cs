using Application.Repositories;
using Application.Services.Abstractions;
using Domain.Common.Results;
using Domain.Entities;

namespace Application.Services;

/// <summary>
/// 監査ログサービスの実装
/// </summary>
public class AuditLogService(IAuditLogRepository repository) : IAuditLogService
{
    /// <inheritdoc />
    public async Task<OperationResult<IEnumerable<AuditLog>>> GetAllAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var auditLogs = await repository.GetAllAsync(limit, cancellationToken);
        return Outcome.Success(auditLogs);
    }
}
