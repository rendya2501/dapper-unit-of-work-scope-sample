using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Contracts.Requests;
using Web.Api.Contracts.Responses;
using Web.Api.Extensions;

namespace Web.Api.Controllers;

/// <summary>
/// 在庫管理APIエンドポイント
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InventoryController(IInventoryService inventoryService) : ControllerBase
{
    /// <summary>
    /// すべての在庫を取得します
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Inventory>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var inventories = await inventoryService.GetAllAsync(cancellationToken);
        return inventories.ToActionResult(this, Ok);
    }

    /// <summary>
    /// 商品IDを指定して在庫を取得します
    /// </summary>
    [HttpGet("{productId}", Name = "GetByProductId")]
    [ProducesResponseType(typeof(Inventory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProductIdAsync(int productId, CancellationToken cancellationToken)
    {
        var inventory = await inventoryService.GetByProductIdAsync(productId, cancellationToken);
        return inventory.ToActionResult(this, Ok);
    }

    /// <summary>
    /// 在庫を作成します
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateInventoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await inventoryService.CreateAsync(
            request.ProductName, request.Stock, request.UnitPrice, cancellationToken);

        return result.ToActionResult(this, productId => CreatedAtAction(
            nameof(GetByProductIdAsync),
            new { productId },
            new CreateInventoryResponse(productId)));
    }

    /// <summary>
    /// 在庫を更新します
    /// </summary>
    [HttpPut("{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(int productId, [FromBody] UpdateInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await inventoryService.UpdateAsync(
            productId, request.ProductName, request.Stock, request.UnitPrice, cancellationToken);

        return result.ToActionResult(this, NoContent);
    }

    /// <summary>
    /// 在庫を削除します
    /// </summary>
    [HttpDelete("{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int productId, CancellationToken cancellationToken)
    {
        var result = await inventoryService.DeleteAsync(productId, cancellationToken);

        return result.ToActionResult(this, NoContent);
    }
}