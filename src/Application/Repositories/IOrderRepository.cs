using Domain.Entities;

namespace Application.Repositories;

/// <summary>
/// 注文リポジトリのインターフェース
/// </summary>
/// <remarks>
/// <para><strong>集約ルートに対するリポジトリ</strong></para>
/// <para>
/// Order は集約ルートであり、OrderDetail を含めて永続化される。
/// OrderDetail 単独のリポジトリは存在しない。
/// </para>
/// 
/// <para><strong>トランザクション管理</strong></para>
/// <para>
/// Repository 自身はトランザクションを開始・終了しない。
/// UnitOfWork が管理する Transaction を利用する。
/// </para>
/// </remarks>
public interface IOrderRepository
{
    /// <summary>
    /// 注文を作成します（注文明細を含む）
    /// </summary>
    /// <param name="order">作成する注文（明細を含む）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>作成された注文のID</returns>
    /// <remarks>
    /// 注文と注文明細を1トランザクションで永続化します。
    /// </remarks>
    Task<int> CreateAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// IDを指定して注文を取得します（注文明細を含む）
    /// </summary>
    /// <param name="id">注文ID</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>注文（明細を含む）。見つからない場合は null</returns>
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// すべての注文を取得します（注文明細を含む）
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>注文のリスト</returns>
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default);
}
