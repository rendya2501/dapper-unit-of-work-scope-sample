namespace Web.Api.Contracts.Responses;

/// <summary>
/// エラーレスポンス
/// </summary>
/// <param name="Error">エラーメッセージ</param>
/// <param name="Details">詳細情報（オプション）</param>
public record ErrorResponse(
    string Message,
    Dictionary<string, string[]>? Errors = null);
