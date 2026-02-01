using Application.Common;
using System.Data;

namespace Infrastructure.Persistence;

/// <summary>
/// データベースセッションの実装クラス。
/// Connection と Transaction の保持のみを担当。
/// ライフサイクル管理は UnitOfWork に委譲。
/// </summary>
public class DbSession(IDbConnection connection) : IDbSessionManager
{
    /// <inheritdoc />
    public IDbConnection Connection => connection;

    /// <inheritdoc />
    public IDbTransaction? Transaction { get; set; }
}
