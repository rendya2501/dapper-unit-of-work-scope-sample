using Microsoft.AspNetCore.Mvc;
using System.Data.Common;

namespace Web.Api.Middleware;

/// <summary>
/// すべての例外を ProblemDetails 形式で返すミドルウェア
/// </summary>
/// <remarks>
/// <para><strong>RFC 7807 準拠</strong></para>
/// <para>
/// ProblemDetails は RFC 7807 で定義された標準フォーマット。
/// すべてのエラーレスポンスを統一することで、
/// クライアント側のエラーハンドリングが容易になる。
/// </para>
/// 
/// <para><strong>設計原則</strong></para>
/// <list type="bullet">
/// <item>すべての例外を1箇所でキャッチ</item>
/// <item>ProblemDetails 形式で統一</item>
/// <item>例外の種類に応じて適切な HTTP ステータスコードを返す</item>
/// <item>本番環境では詳細なエラー情報を隠蔽</item>
/// </list>
/// </remarks>
public class ProblemDetailsMiddleware(
    RequestDelegate next,
    ILogger<ProblemDetailsMiddleware> logger,
    IHostEnvironment environment)
{
    /// <summary>
    /// 次のミドルウェアを実行し、パイプライン内で発生したすべての例外を捕捉する。
    /// </summary>
    /// <param name="context">HTTP リクエストコンテキスト</param>
    /// <remarks>
    /// このメソッドはミドルウェアのエントリーポイントであり、
    /// 例外はここで必ず捕捉され <see cref="HandleExceptionAsync"/> に委譲される。
    /// </remarks>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// 捕捉した例外を ProblemDetails 形式の HTTP レスポンスに変換する。
    /// </summary>
    /// <param name="context">HTTP コンテキスト</param>
    /// <param name="exception">発生した例外</param>
    /// <remarks>
    /// 例外の種類に応じて HTTPステータスコード、タイトル、詳細、拡張情報を決定する。
    /// </remarks>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // 予期しない例外のみを処理
        var (statusCode, title, detail) = exception switch
        {
            // データベース接続エラーなど、インフラ層の例外
            DbException dbEx => (
                StatusCodes.Status503ServiceUnavailable,
                "Service Unavailable",
                environment.IsDevelopment() ? dbEx.Message : "Database temporarily unavailable"
            ),

            // タイムアウト
            TimeoutException => (
                StatusCodes.Status504GatewayTimeout,
                "Request Timeout",
                "The request took too long to process"
            ),

            // その他すべての予期しない例外
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                environment.IsDevelopment() ? exception.Message : "An unexpected error occurred"
            )
        };

        logger.LogError(exception, "Unhandled exception occurred");

        // ProblemDetails レスポンス作成
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = GetProblemType(statusCode)
        };

        if (environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    /// <summary>
    /// HTTP ステータスコードに対応する RFC 参照 URI を取得する。
    /// </summary>
    /// <param name="statusCode">HTTP ステータスコード</param>
    /// <returns>ProblemDetails の type フィールドに設定する URI</returns>
    private static string GetProblemType(int statusCode) => statusCode switch
    {
        StatusCodes.Status500InternalServerError => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        StatusCodes.Status503ServiceUnavailable => "https://tools.ietf.org/html/rfc7231#section-6.6.4",
        StatusCodes.Status504GatewayTimeout => "https://tools.ietf.org/html/rfc7231#section-6.6.5",
        _ => "https://tools.ietf.org/html/rfc7231"
    };
}
