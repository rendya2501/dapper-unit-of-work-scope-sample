using Domain.Common.Results;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Contracts.Responses;

namespace Web.Api.Extensions;

/// <summary>
/// <see cref="OperationResult{T}"/> および <see cref="OperationResult"/> を
/// ASP.NET Core の <see cref="IActionResult"/> に変換するための拡張メソッドを提供します。
/// </summary>
public static class OperationResultExtensions
{
    /// <summary>
    /// 値を持つ操作結果を <see cref="IActionResult"/> に変換します。
    /// </summary>
    /// <typeparam name="T">操作結果に含まれる値の型</typeparam>
    /// <param name="result">変換元の操作結果</param>
    /// <param name="controller">現在のコントローラーインスタンス</param>
    /// <param name="onSuccess">成功時（値あり）に実行されるアクション。通常は <c>Ok(value)</c> や <c>CreatedAtAction(...)</c> を指定します。</param>
    /// <returns>
    /// 成功時は <paramref name="onSuccess"/> の結果、
    /// SuccessEmpty の場合は 204 No Content、
    /// 失敗時はエラー内容に応じた適切な HTTP ステータスコードの結果を返します。
    /// </returns>
    public static IActionResult ToActionResult<T>(
        this OperationResult<T> result,
        ControllerBase controller,
        Func<T, IActionResult> onSuccess)
    {
        return result.Match(
            onSuccess: onSuccess,
            onSuccessEmpty: controller.NoContent,
            onFailure: failure => HandleError(controller, failure));
    }

    /// <summary>
    /// 値を持たない操作結果を <see cref="IActionResult"/> に変換します。
    /// </summary>
    /// <param name="result">変換元の操作結果</param>
    /// <param name="controller">現在のコントローラーインスタンス</param>
    /// <param name="onSuccess">
    /// 成功時に実行されるアクション。省略した場合は 204 No Content を返します。
    /// </ tank>
    /// <returns>
    /// 成功時は <paramref name="onSuccess"/> の結果（または 204 No Content）、
    /// 失敗時はエラー内容に応じた適切な HTTP ステータスコードの結果を返します。
    /// </returns>
    public static IActionResult ToActionResult(
        this OperationResult result,
        ControllerBase controller,
        Func<IActionResult>? onSuccess = null)
    {
        return result.Match(
            onSuccess: onSuccess ?? (() => controller.NoContent()),
            onFailure: failure => HandleError(controller, failure));
    }

    /// <summary>
    /// <see cref="OperationError"/> の型に基づいて、適切な HTTP ステータスコードを持つ <see cref="IActionResult"/> を生成します。
    /// </summary>
    /// <param name="controller">コントローラーインスタンス</param>
    /// <param name="error">エラー情報</param>
    /// <returns>
    /// NotFound(404), BadRequest(400), Conflict(409), Unauthorized(401), Forbidden(403), 
    /// または InternalServerError(500) のいずれかを返します。
    /// </returns>
    private static IActionResult HandleError(
        ControllerBase controller,
        OperationError error)
    {
        return error switch
        {
            // 404 Not Found: リソースが見つからない場合
            OperationError.NotFound nf => controller.NotFound(
                new ErrorResponse(nf.Message ?? "Resource not found")),

            // 400 Bad Request: 入力バリデーションエラー
            OperationError.ValidationFailed vf => controller.BadRequest(
                new ErrorResponse("Validation failed", vf.Errors)),

            // 409 Conflict: リソースの状態競合（重複、不正な状態遷移など）
            OperationError.Conflict c => controller.Conflict(
                new ErrorResponse(c.Message)),

            // 400 Bad Request: ビジネスルール違反。エラーコードを含めて返却
            OperationError.BusinessRule br => controller.BadRequest(
                new BusinessErrorResponse(br.Code, br.Message)),

            // 401 Unauthorized: 認証が必要な場合
            OperationError.Unauthorized _ => controller.Unauthorized(),

            // 403 Forbidden: 権限が不足している場合
            OperationError.Forbidden f => controller.StatusCode(StatusCodes.Status403Forbidden,
                new ErrorResponse(f.Message)),

            // 500 Internal Server Error: 未定義のエラー
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse("Internal server error"))
        };
    }
}
