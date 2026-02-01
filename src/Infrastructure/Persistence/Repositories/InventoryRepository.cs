using Application.Common;
using Application.Repositories;
using Dapper;
using Domain.Entities;

namespace Infrastructure.Persistence.Repositories;

/// <summary>
/// 在庫リポジトリの実装
/// </summary>
/// <remarks>
/// <para><strong>設計原則</strong></para>
/// <list type="bullet">
/// <item>Repository は session.Connection と session.Transaction を受け取るが、Begin/Commit/Rollback は一切行わない</item>
/// <item>トランザクション管理は UnitOfWork が責任を持つ</item>
/// <item>Repository は純粋にデータアクセスのみに専念</item>
/// </list>
/// </remarks>
/// <param name="session.Connection">データベース接続</param>
/// <param name="session.Transaction">トランザクション（UnitOfWork から注入）</param>
public class InventoryRepository(IDbSession session)
    : IInventoryRepository
{
    /// <inheritdoc />
    public async Task<Inventory?> GetByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM Inventory WHERE ProductId = @ProductId";

        var command = new CommandDefinition(
            sql,
            new { ProductId = productId },
            session.Transaction,
            cancellationToken: cancellationToken);

        return await session.Connection.QueryFirstOrDefaultAsync<Inventory>(command);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Inventory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM Inventory ORDER BY ProductId";

        var command = new CommandDefinition(
            sql,
            session.Transaction,
            cancellationToken: cancellationToken);

        return await session.Connection.QueryAsync<Inventory>(command);
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(
        Inventory inventory,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Inventory (ProductName, Stock, UnitPrice)
            VALUES (@ProductName, @Stock, @UnitPrice);
            SELECT last_insert_rowid();
            """;

        var command = new CommandDefinition(
            sql,
            inventory,
            session.Transaction,
            cancellationToken: cancellationToken);

        return await session.Connection.ExecuteScalarAsync<int>(command);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        int productId,
        string productName,
        int stock,
        decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Inventory 
            SET ProductName = @ProductName, Stock = @Stock, UnitPrice = @UnitPrice
            WHERE ProductId = @ProductId
            """;

        var command = new CommandDefinition(
            sql,
            new { ProductId = productId, ProductName = productName, Stock = stock, UnitPrice = unitPrice },
            session.Transaction,
            cancellationToken: cancellationToken);

        await session.Connection.ExecuteAsync(command);
    }

    /// <inheritdoc />
    public async Task<int> UpdateStockAsync(
        int productId,
        int newStock,
        CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Inventory SET Stock = @Stock WHERE ProductId = @ProductId";

        var command = new CommandDefinition(
            sql,
            new { ProductId = productId, Stock = newStock },
            session.Transaction,
            cancellationToken: cancellationToken);

        return await session.Connection.ExecuteAsync(command);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int productId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Inventory WHERE ProductId = @ProductId";

        var command = new CommandDefinition(
            sql,
            new { ProductId = productId },
            session.Transaction,
            cancellationToken: cancellationToken);

        await session.Connection.ExecuteAsync(command);
    }
}