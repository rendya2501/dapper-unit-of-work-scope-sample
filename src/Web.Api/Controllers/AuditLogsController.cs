using Application.Services.Abstractions;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Extensions;

namespace Web.Api.Controllers;

/// <summary>
/// 監査ログAPIエンドポイント
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuditLogsController(IAuditLogService auditLogService) : ControllerBase
{
    /// <summary>
    /// すべての監査ログを取得します
    /// </summary>
    /// <param name="limit">取得件数の上限（デフォルト: 100）</param>
    /// <returns>監査ログのリスト（新しい順）</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AuditLog>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var logs = await auditLogService.GetAllAsync(limit, cancellationToken);
        return logs.ToActionResult(this, Ok);
    }
}
