using Domain.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Extensions;

/// <summary>
/// <see cref="Error"/> を RFC 7807 準拠の <see cref="IActionResult"/> に変換するクラス
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ResultHttpExtensions"/> の失敗パスから呼び出される内部ヘルパーです。
/// エンドポイントから直接呼び出すことは想定していません。
/// </para>
/// <para>
/// <strong>ErrorType と HTTP ステータスコードのマッピング:</strong><br/>
/// - <see cref="ErrorType.Validation"/> / <see cref="ErrorType.Problem"/> → 400 Bad Request<br/>
/// - <see cref="ErrorType.NotFound"/> → 404 Not Found<br/>
/// - <see cref="ErrorType.Conflict"/> → 409 Conflict<br/>
/// - <see cref="ErrorType.Failure"/> → 500 Internal Server Error
/// </para>
/// </remarks>
internal static class ErrorToProblemMapper
{
    /// <summary>
    /// <see cref="Error"/> を ProblemDetails 形式の <see cref="IActionResult"/> に変換します
    /// </summary>
    /// <param name="error">変換対象のエラー</param>
    /// <returns>ProblemDetails を含む <see cref="IActionResult"/></returns>
    internal static IActionResult ToActionResult(Error error)
    {
        var status = GetStatusCode(error.Type);

        var problemDetails = new ProblemDetails
        {
            Title = GetTitle(error),
            Detail = GetDetail(error),
            Type = GetType(error.Type),
            Status = status
        };

        var extensions = GetExtensions(error);
        if (extensions is not null)
        {
            foreach (var (key, value) in extensions)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = status
        };
    }

    /// <summary>
    /// エラータイプに応じたタイトルを返します
    /// </summary>
    /// <param name="error">対象エラー</param>
    /// <returns>ProblemDetails の Title フィールドに使用する文字列</returns>
    private static string GetTitle(Error error) =>
        error.Type switch
        {
            ErrorType.Failure => "Server failure",
            _ => error.Code
        };

    /// <summary>
    /// エラータイプに応じた詳細説明を返します
    /// </summary>
    /// <param name="error">対象エラー</param>
    /// <returns>ProblemDetails の Detail フィールドに使用する文字列</returns>
    private static string GetDetail(Error error) =>
        error.Type switch
        {
            ErrorType.Failure => "An unexpected error occurred",
            _ => error.Description
        };

    /// <summary>
    /// エラータイプに応じた RFC 7807 の type URI を返します
    /// </summary>
    /// <param name="errorType">エラータイプ</param>
    /// <returns>ProblemDetails の Type フィールドに使用する URI 文字列</returns>
    private static string GetType(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation or ErrorType.Problem
                => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.NotFound
                => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            ErrorType.Conflict
                => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

    /// <summary>
    /// エラータイプに応じた HTTP ステータスコードを返します
    /// </summary>
    /// <param name="errorType">エラータイプ</param>
    /// <returns>HTTP ステータスコード</returns>
    private static int GetStatusCode(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation or ErrorType.Problem => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

    /// <summary>
    /// <see cref="ValidationError"/> の場合に個別エラーを extensions として返します
    /// </summary>
    /// <param name="error">対象エラー</param>
    /// <returns>
    /// <paramref name="error"/> が <see cref="ValidationError"/> の場合は個別エラーを含む辞書、
    /// それ以外の場合は <see langword="null"/>
    /// </returns>
    private static Dictionary<string, object?>? GetExtensions(Error error)
    {
        if (error is not ValidationError validationError)
        {
            return null;
        }

        return new Dictionary<string, object?>
        {
            { "errors", validationError.Errors }
        };
    }
}
