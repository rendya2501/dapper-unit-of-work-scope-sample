namespace Domain.Common.Results;

/// <summary>
/// 操作結果を生成するファクトリクラス
/// </summary>
/// <remarks>
/// <para><strong>命名の理由</strong></para>
/// <list type="bullet">
/// <item>"Outcome" = 操作の「結果・成果」を表す自然な英語表現</item>
/// <item>戻り値の Result 型と名前が衝突しない</item>
/// </list>
/// 
/// <para><strong>使用例</strong></para>
/// <code>
/// // 成功
/// return Outcome.Success(order);           // 値あり
/// return Outcome.Success();                // 値なし
/// 
/// // 失敗
/// return Outcome.NotFound("Order not found");
/// return Outcome.BusinessRule(
///     BusinessErrorCode.InsufficientStock.ToErrorCode(),
///     $"Available: {stock}, Requested: {quantity}");
/// </code>
/// </remarks>
public static class Outcome
{
    // ========================================
    // 成功系ファクトリメソッド
    // ========================================

    /// <summary>
    /// 値を持つ成功結果を生成
    /// </summary>
    public static OperationResult<T> Success<T>(T value)
        =>Success(value);

    /// <summary>
    /// 値を持たない成功結果を生成
    /// </summary>
    public static OperationResult Success()
        => OperationResult.Success();


    // ========================================
    // エラー系ファクトリメソッド
    // ========================================

    /// <summary>
    /// リソースが見つからないエラーを生成 (404 Not Found)
    /// </summary>
    public static OperationError NotFound(string? message = null)
        => new OperationError.NotFound(message);

    /// <summary>
    /// 複数フィールドのバリデーションエラーを生成 (400 Bad Request)
    /// </summary>
    public static OperationError ValidationFailed(Dictionary<string, string[]> errors)
        => new OperationError.ValidationFailed(errors);

    /// <summary>
    /// 単一フィールドのバリデーションエラーを生成 (400 Bad Request)
    /// </summary>
    public static OperationError ValidationFailed(string field, string error)
        => new OperationError.ValidationFailed(
            new Dictionary<string, string[]> { [field] = [error] });

    /// <summary>
    /// リソースの状態競合エラーを生成 (409 Conflict)
    /// </summary>
    public static OperationError Conflict(string message)
        => new OperationError.Conflict(message);

    /// <summary>
    /// ビジネスルール違反エラーを生成 (400 Bad Request)
    /// </summary>
    public static OperationError BusinessRule(string code, string message)
        => new OperationError.BusinessRule(code, message);

    /// <summary>
    /// 認証が必要なエラーを生成 (401 Unauthorized)
    /// </summary>
    public static OperationError Unauthorized(string message = "Authentication required")
        => new OperationError.Unauthorized(message);

    /// <summary>
    /// 権限不足エラーを生成 (403 Forbidden)
    /// </summary>
    public static OperationError Forbidden(string message = "Insufficient permissions")
        => new OperationError.Forbidden(message);
}
