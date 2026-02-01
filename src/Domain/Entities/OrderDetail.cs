namespace Domain.Entities;

/// <summary>
/// 注文明細エンティティ
/// </summary>
/// <remarks>
/// <para><strong>値オブジェクトに近い性質</strong></para>
/// <para>
/// OrderDetail は Order の一部であり、Order なしでは意味を持たない。
/// 独立した Repository は存在せず、Order を通じてのみ永続化される。
/// </para>
/// </remarks>
public class OrderDetail
{
    /// <summary>
    /// 注文明細ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 注文ID（外部キー）
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// 商品ID
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 数量
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 単価
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 小計
    /// </summary>
    /// <remarks>
    /// 計算プロパティ。DB には永続化しない。
    /// </remarks>
    public decimal SubTotal => UnitPrice * Quantity;
}
