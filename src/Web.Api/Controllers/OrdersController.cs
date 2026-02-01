using Application.Models;
using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Contracts.Requests;
using Web.Api.Contracts.Responses;
using Web.Api.Extensions;

namespace Web.Api.Controllers;

/// <summary>
/// 注文関連のAPIエンドポイント
/// </summary>
/// <param name="orderService">注文サービス</param>
[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    /// <summary>
    /// 注文を作成します
    /// </summary>
    /// <param name="request">注文作成リクエスト</param>
    /// <returns>作成された注文のID</returns>
    /// <response code="200">注文が正常に作成されました</response>
    /// <response code="400">リクエストが不正です</response>
    /// <response code="500">内部サーバーエラー</response>
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrderAsync([FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        // バリデーションは ValidationFilter が自動実行
        // エラーは ProblemDetailsMiddleware が自動変換

        var items = request.Items
            .Select(i => new OrderItem(i.ProductId, i.Quantity))
            .ToList();

        var result = await orderService.CreateOrderAsync(request.CustomerId, items, cancellationToken);

        return result.ToActionResult(this, orderId => CreatedAtAction(
            nameof(GetOrderByIdAsync),
            new { id = orderId },
            new CreateOrderResponse(orderId)));
    }

    /// <summary>
    /// すべての注文を取得します
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Order>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrdersAsync(CancellationToken cancellationToken)
    {
        var result = await orderService.GetAllOrdersAsync(cancellationToken);
        return result.ToActionResult(this, Ok);
    }

    /// <summary>
    /// IDを指定して注文を取得します
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await orderService.GetOrderByIdAsync(id, cancellationToken);
        return result.ToActionResult(this, Ok);
    }
}
