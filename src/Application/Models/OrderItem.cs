namespace Application.Models;

/// <summary>
/// 注文アイテム（Application層のDTO）
/// </summary>
/// <remarks>
/// <para><strong>配置場所の理由</strong></para>
/// <para>
/// OrderItem は Application層のサービス契約の一部であり、
/// Controller（API層）と Service（Application層）の間でやり取りされる。
/// Application.Models に配置することで、層の責務が明確になる。
/// </para>
/// </remarks>
/// <param name="ProductId">商品ID</param>
/// <param name="Quantity">数量</param>
public record OrderItem(int ProductId, int Quantity);
