using Application.Common;
using Application.Repositories;
using Dapper;
using Domain.Entities;

namespace Infrastructure.Persistence.Repositories;

/// <summary>
/// 監査ログリポジトリの実装
/// </summary>
public class AuditLogRepository(IDbSession session)
    : IAuditLogRepository
{
    /// <inheritdoc />
    public async Task CreateAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO AuditLog (Action, Details, CreatedAt)
            VALUES (@Action, @Details, @CreatedAt)
            """;

        var command = new CommandDefinition(
            sql,
            log,
            session.Transaction,
            cancellationToken: cancellationToken);

        await session.Connection.ExecuteAsync(command);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAllAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT * FROM AuditLog 
            ORDER BY CreatedAt DESC 
            LIMIT @Limit
            """;

        var command = new CommandDefinition(
            sql,
            new { Limit = limit },
            session.Transaction,
            cancellationToken: cancellationToken);

        return await session.Connection.QueryAsync<AuditLog>(command);
    }
}
