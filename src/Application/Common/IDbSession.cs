using System.Data;

namespace Application.Common;

/// <summary>
/// 現在のデータベースセッション（接続およびトランザクション）へのアクセスを提供します。
/// </summary>
/// <remarks>
/// このアクセサーは通常、DIコンテナによってスコープ単位（HTTPリクエスト等）で管理され、
/// 複数のリポジトリやサービス間で同一の接続およびトランザクションを共有するために使用されます。
/// </remarks>
public interface IDbSession
{
    /// <summary>
    /// 現在のデータベース接続を取得します。
    /// </summary>
    IDbConnection Connection { get; }

    /// <summary>
    /// 現在開始されているトランザクションを取得します。
    /// </summary>
    IDbTransaction? Transaction { get; }
}
