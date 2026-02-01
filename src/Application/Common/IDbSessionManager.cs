using System.Data;

namespace Application.Common;

/// <summary>
/// データベースセッションの管理用インターフェース。
/// UnitOfWork などの「管理者」に対して公開されます。
/// </summary>
public interface IDbSessionManager : IDbSession
{
    /// <summary>
    /// 現在のトランザクションを取得または設定します。
    /// </summary>
    new IDbTransaction? Transaction { get; set; }
}