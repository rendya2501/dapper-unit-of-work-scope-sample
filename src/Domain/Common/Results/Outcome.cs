namespace Domain.Common.Results;

/// <summary>
/// 操作結果を生成するファクトリクラス
/// </summary>
/// <remarks>
/// <para><strong>命名の理由</strong></para>
/// <list type="bullet">
/// <item>"Outcome" = 操作の「結果・成果」を表す自然な英語表現</item>
/// <item>戻り値の Result 型と名前が衝突しない</item>
/// <item>Try, Succeed, Fail などの動詞と自然に組み合わせられる</item>
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
/// 
/// <para><strong>設計原則</strong></para>
/// <list type="bullet">
/// <item>各メソッドは対応する OperationError を生成</item>
/// <item>型推論により OperationResult&lt;T&gt; への暗黙的変換が行われる</item>
/// <item>一貫性のあるAPI設計により、学習コストを最小化</item>
/// </list>
/// </remarks>
public static class Outcome
{
    // ========================================
    // 成功系ファクトリメソッド
    // ========================================

    /// <summary>
    /// 値を持つ成功結果を生成
    /// </summary>
    /// <typeparam name="T">戻り値の型</typeparam>
    /// <param name="value">成功時の戻り値</param>
    /// <returns>成功結果</returns>
    /// <remarks>
    /// <example>
    /// <code>
    /// var product = await repository.GetByIdAsync(id);
    /// return Outcome.Success(product);
    /// // → OperationResult&lt;Product&gt;.Success
    /// </code>
    /// </example>
    /// </remarks>
    public static OperationResult<T> Success<T>(T value)
        => new OperationResult<T>.Success(value);

    /// <summary>
    /// 値を持たない成功結果を生成
    /// </summary>
    /// <returns>成功結果</returns>
    /// <remarks>
    /// DELETE操作など、値を返す必要がない場合に使用。
    /// HTTP 204 No Content に対応。
    /// <example>
    /// <code>
    /// await repository.DeleteAsync(id);
    /// return Outcome.Success();
    /// // → OperationResult.Success (204 No Content)
    /// </code>
    /// </example>
    /// </remarks>
    public static OperationResult Success()
        => new OperationResult.Success();


    // ========================================
    // リソース系エラーファクトリメソッド
    // ========================================

    /// <summary>
    /// リソースが見つからないエラーを生成 (404 Not Found)
    /// </summary>
    /// <param name="message">エラーメッセージ。nullの場合は "Resource not found" が使用される</param>
    /// <returns>NotFoundエラー</returns>
    /// <remarks>
    /// <example>
    /// <code>
    /// var order = await repository.GetByIdAsync(id);
    /// if (order == null)
    ///     return Outcome.NotFound($"Order {id} not found");
    /// </code>
    /// </example>
    /// </remarks>
    public static OperationError NotFound(string? message = null)
        => new OperationError.NotFound(message);


    // ========================================
    // バリデーション系エラーファクトリメソッド
    // ========================================

    /// <summary>
    /// 複数フィールドのバリデーションエラーを生成 (400 Bad Request)
    /// </summary>
    /// <param name="errors">フィールド名とエラーメッセージのマッピング</param>
    /// <returns>ValidationFailedエラー</returns>
    /// <remarks>
    /// 通常は FluentValidation の ValidationFilter で自動生成される。
    /// 手動でバリデーションを実装する場合のみ使用。
    /// <example>
    /// <code>
    /// var errors = new Dictionary&lt;string, string[]&gt;
    /// {
    ///     ["ProductName"] = ["Product name is required"],
    ///     ["Stock"] = ["Stock must be greater than or equal to 0"]
    /// };
    /// return Outcome.ValidationFailed(errors);
    /// </code>
    /// </example>
    /// </remarks>
    public static OperationError ValidationFailed(Dictionary<string, string[]> errors)
        => new OperationError.ValidationFailed(errors);

    /// <summary>
    /// 単一フィールドのバリデーションエラーを生成 (400 Bad Request)
    /// </summary>
    /// <param name="field">エラーが発生したフィールド名</param>
    /// <param name="error">エラーメッセージ</param>
    /// <returns>ValidationFailedエラー</returns>
    /// <remarks>
    /// <example>
    /// <code>
    /// if (string.IsNullOrEmpty(productName))
    ///     return Outcome.ValidationFailed("ProductName", "Product name is required");
    /// </code>
    /// </example>
    /// </remarks>
    public static OperationError ValidationFailed(string field, string error)
        => new OperationError.ValidationFailed(
            new Dictionary<string, string[]> { [field] = [error] });


    // ========================================
    // ビジネスルール違反エラーファクトリメソッド
    // ========================================

    /// <summary>
    /// リソースの状態競合エラーを生成 (409 Conflict)
    /// </summary>
    /// <param name="message">競合の詳細を説明するメッセージ</param>
    /// <returns>Conflictエラー</returns>
    /// <remarks>
    /// <para><strong>使用ケース</strong></para>
    /// <list type="bullet">
    /// <item>同じ名前のリソースが既に存在する</item>
    /// <item>注文が既にキャンセル済みで変更不可</item>
    /// <item>楽観的ロック違反（バージョン不一致）</item>
    /// <item>状態遷移が不正（発送済み→未発送に戻せない）</item>
    /// </list>
    /// <example>
    /// <code>
    /// var existing = await repository.FindByNameAsync(name);
    /// if (existing != null)
    ///     return Outcome.Conflict($"Product '{name}' already exists");
    /// 
    /// if (order.Status == OrderStatus.Shipped)
    ///     return Outcome.Conflict("Cannot modify shipped orders");
    /// </code>
    /// </example>
    /// </remarks>
    public static OperationError Conflict(string message)
        => new OperationError.Conflict(message);


    /// <summary>
    /// ビジネスルール違反エラーを生成 (400 Bad Request)
    /// </summary>
    /// <param name="code">エラーコード（enum値の文字列表現を推奨）</param>
    /// <param name="message">ユーザー向けのエラーメッセージ</param>
    /// <returns>BusinessRuleエラー</returns>
    /// <remarks>
    /// <para><strong>Conflict との使い分け</strong></para>
    /// <list type="bullet">
    /// <item><c>Conflict</c>: リソースの「状態」が原因（既存データとの衝突）</item>
    /// <item><c>BusinessRule</c>: ビジネス「条件」が原因（在庫不足、期限切れなど）</item>
    /// </list>
    /// 
    /// <para><strong>Code の推奨パターン</strong></para>
    /// <list type="bullet">
    /// <item>enum で定義し、拡張メソッドで UPPER_SNAKE_CASE に変換</item>
    /// <item>フロントエンドで条件分岐可能な一意の識別子</item>
    /// <item>ハードコーディングではなく、定数または enum を使用</item>
    /// </list>
    /// <example>
    /// <code>
    /// // Enum定義（推奨）
    /// public enum BusinessErrorCode
    /// {
    ///     InsufficientStock,
    ///     InvalidQuantity,
    ///     OrderExpired
    /// }
    /// 
    /// // 使用例
    /// if (product.Stock &lt; quantity)
    ///     return Outcome.BusinessRule(
    ///         BusinessErrorCode.InsufficientStock.ToErrorCode(), // "INSUFFICIENT_STOCK"
    ///         $"Insufficient stock for {product.Name}. Available: {product.Stock}, Requested: {quantity}");
    /// 
    /// if (quantity &lt;= 0)
    ///     return Outcome.BusinessRule(
    ///         BusinessErrorCode.InvalidQuantity.ToErrorCode(),
    ///         "Quantity must be greater than 0");
    /// 
    /// // フロントエンドでの処理例 (TypeScript)
    /// if (error.code === 'INSUFFICIENT_STOCK') {
    ///     showNotification('在庫不足です。数量を減らしてください。');
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public static OperationError BusinessRule(string code, string message)
        => new OperationError.BusinessRule(code, message);


    // ========================================
    // 権限系エラーファクトリメソッド
    // ========================================

    /// <summary>
    /// 認証が必要なエラーを生成 (401 Unauthorized)
    /// </summary>
    /// <param name="message">認証エラーの詳細。デフォルトは "Authentication required"</param>
    /// <returns>Unauthorizedエラー</returns>
    /// <remarks>
    /// ユーザーがログインしていない、トークンが無効などの場合に使用。
    /// 通常は認証ミドルウェアで処理されるが、サービス層でも使用可能。
    /// <example>
    /// <code>
    /// if (currentUser == null)
    ///     return Outcome.Unauthorized("Please login to continue");
    /// 
    /// if (token.IsExpired)
    ///     return Outcome.Unauthorized("Token expired. Please login again");
    /// </code>
    /// </example>
    /// </remarks>
    public static OperationError Unauthorized(string message = "Authentication required")
        => new OperationError.Unauthorized(message);

    /// <summary>
    /// 権限不足エラーを生成 (403 Forbidden)
    /// </summary>
    /// <param name="message">権限エラーの詳細。デフォルトは "Insufficient permissions"</param>
    /// <returns>Forbiddenエラー</returns>
    /// <remarks>
    /// ユーザーは認証済みだが、操作を実行する権限がない場合に使用。
    /// <example>
    /// <code>
    /// if (!currentUser.IsAdmin)
    ///     return Outcome.Forbidden("Only administrators can delete products");
    /// 
    /// if (order.CustomerId != currentUser.Id)
    ///     return Outcome.Forbidden("You can only view your own orders");
    /// </code>
    /// </example>
    /// </remarks>
    public static OperationError Forbidden(string message = "Insufficient permissions")
        => new OperationError.Forbidden(message);
}
