namespace Domain.Common.Results;

/// <summary>
/// 操作失敗の理由を表す識別可能な型
/// </summary>
/// <remarks>
/// <para><strong>設計原則</strong></para>
/// <list type="bullet">
/// <item>各エラー型は sealed record として定義し、継承を防止</item>
/// <item>HTTP ステータスコードとの対応を明確化</item>
/// <item>クライアントが種類に応じた処理を実装可能</item>
/// </list>
/// 
/// <para><strong>使用例</strong></para>
/// <code>
/// // サービス層
/// if (product == null)
///     return Outcome.NotFound($"Product {id} not found");
/// 
/// if (stock &lt; quantity)
///     return Outcome.BusinessRule(
///         BusinessErrorCode.InsufficientStock,
///         $"Available: {stock}, Requested: {quantity}");
/// 
/// // コントローラー層（自動変換）
/// return result.ToActionResult(this, id => Ok(id));
/// // → 404 Not Found または 400 Bad Request (INSUFFICIENT_STOCK)
/// </code>
/// </remarks>
public abstract record OperationError
{
    // ========================================
    // リソース系エラー (4xx)
    // ========================================

    /// <summary>
    /// 指定されたリソースが見つからない (404 Not Found)
    /// </summary>
    /// <param name="Message">エラーメッセージ。nullの場合は "Resource not found" が使用される</param>
    /// <example>
    /// <code>
    /// var product = await repository.GetByIdAsync(id);
    /// if (product == null)
    ///     return Outcome.NotFound($"Product {id} not found");
    /// </code>
    /// </example>
    public sealed record NotFound(string? Message = null) : OperationError;


    // ========================================
    // バリデーション系エラー (400)
    // ========================================

    /// <summary>
    /// 入力データの形式・内容が不正 (400 Bad Request)
    /// </summary>
    /// <param name="Errors">フィールド名とエラーメッセージのマッピング</param>
    /// <remarks>
    /// FluentValidation のバリデーションエラーを表現するために使用。
    /// 通常は ValidationFilter で自動的に生成される。
    /// </remarks>
    /// <example>
    /// <code>
    /// var errors = new Dictionary&lt;string, string[]&gt;
    /// {
    ///     ["ProductName"] = ["Product name is required"],
    ///     ["Price"] = ["Price must be greater than 0"]
    /// };
    /// return Outcome.ValidationFailed(errors);
    /// </code>
    /// </example>
    public sealed record ValidationFailed(Dictionary<string, string[]> Errors) : OperationError;


    // ========================================
    // ビジネスルール違反エラー (400/409)
    // ========================================

    /// <summary>
    /// リソースの状態が操作を許可しない (409 Conflict)
    /// </summary>
    /// <param name="Message">競合の詳細を説明するメッセージ</param>
    /// <remarks>
    /// <para><strong>使用ケース</strong></para>
    /// <list type="bullet">
    /// <item>同じ名前のリソースが既に存在する</item>
    /// <item>注文が既にキャンセル済みで変更不可</item>
    /// <item>リソースがロックされている</item>
    /// <item>状態遷移が不正（例: 発送済み→未発送に戻せない）</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var existing = await repository.FindByNameAsync(productName);
    /// if (existing != null)
    ///     return Outcome.Conflict($"Product '{productName}' already exists");
    /// </code>
    /// </example>
    public sealed record Conflict(string Message) : OperationError;

    /// <summary>
    /// ビジネスルールに違反している (400 Bad Request)
    /// </summary>
    /// <param name="Code">エラーコード (enum値の文字列表現を推奨)</param>
    /// <param name="Message">ユーザー向けのエラーメッセージ</param>
    /// <remarks>
    /// <para><strong>Conflict との使い分け</strong></para>
    /// <list type="bullet">
    /// <item><c>Conflict</c>: リソースの「状態」が原因（既存データとの衝突）</item>
    /// <item><c>BusinessRule</c>: ビジネス「条件」が原因（在庫不足、期限切れなど）</item>
    /// </list>
    /// 
    /// <para><strong>Code の命名規則</strong></para>
    /// <list type="bullet">
    /// <item>UPPER_SNAKE_CASE を使用</item>
    /// <item>enum で定義し、ToString() で文字列化を推奨</item>
    /// <item>フロントエンドで条件分岐可能な一意の識別子とする</item>
    /// </list>
    /// </remarks>
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
    ///         BusinessErrorCode.InsufficientStock.ToString(),
    ///         $"Insufficient stock for {product.Name}. Available: {product.Stock}");
    /// 
    /// // フロントエンドでの処理例 (TypeScript)
    /// if (error.code === 'InsufficientStock') {
    ///     showNotification('在庫不足です。数量を減らしてください。');
    /// }
    /// </code>
    /// </example>
    public sealed record BusinessRule(string Code, string Message) : OperationError;


    // ========================================
    // 権限系エラー (401/403)
    // ========================================

    /// <summary>
    /// 認証が必要 (401 Unauthorized)
    /// </summary>
    /// <param name="Message">認証エラーの詳細。デフォルトは "Authentication required"</param>
    /// <example>
    /// <code>
    /// if (currentUser == null)
    ///     return Outcome.Unauthorized("Please login to continue");
    /// </code>
    /// </example>
    public sealed record Unauthorized(string Message = "Authentication required") : OperationError;

    /// <summary>
    /// 権限がない (403 Forbidden)
    /// </summary>
    /// <param name="Message">権限エラーの詳細。デフォルトは "Insufficient permissions"</param>
    /// <example>
    /// <code>
    /// if (!currentUser.IsAdmin)
    ///     return Outcome.Forbidden("Only administrators can delete products");
    /// </code>
    /// </example>
    public sealed record Forbidden(string Message = "Insufficient permissions") : OperationError;
}
