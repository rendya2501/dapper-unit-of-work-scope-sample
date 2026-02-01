namespace Domain.Entities;

/// <summary>
/// 在庫エンティティ
/// </summary>
public class Inventory
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 在庫数
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// 商品名
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 単価
    /// </summary>
    public decimal UnitPrice { get; set; }
}
