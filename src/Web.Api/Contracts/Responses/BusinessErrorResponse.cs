namespace Web.Api.Contracts.Responses;

/// <summary>
/// ビジネスルール違反専用のレスポンス
/// フロントエンドでエラーコードを使った個別ハンドリングが可能
/// </summary>
/// <remarks>
/// 例: フロントエンドでの使用
/// if (error.code === 'INSUFFICIENT_STOCK') {
///   showStockNotification();
/// }
/// </remarks>
public record BusinessErrorResponse(string Code, string Message);
