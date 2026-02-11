namespace Web.Api.Contracts.Inventory.Responses;

/// <summary>
/// 在庫情報のレスポンスDTO
/// </summary>
/// <param name="ProductId">商品ID</param>
/// <param name="ProductName">商品名</param>
/// <param name="Stock">現在の在庫数</param>
/// <param name="UnitPrice">単価</param>
public record InventoryResponse(
    int ProductId,
    string ProductName,
    int Stock,
    decimal UnitPrice);
