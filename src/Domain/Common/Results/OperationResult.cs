namespace Domain.Common.Results;

/// <summary>
/// 値を返す操作の結果を表現する型
/// </summary>
/// <typeparam name="T">成功時に返す値の型</typeparam>
/// <remarks>
/// <para><strong>設計原則</strong></para>
/// <list type="bullet">
/// <item>Success: 値を持つ成功</item>
/// <item>SuccessEmpty: 値を持たない成功（204 No Content用）</item>
/// <item>Failure: エラー情報を持つ失敗</item>
/// </list>
/// 
/// <para><strong>使用例</strong></para>
/// <code>
/// // サービス層
/// public async Task&lt;OperationResult&lt;Order&gt;&gt; GetOrderByIdAsync(int id)
/// {
///     var order = await repository.GetByIdAsync(id);
///     if (order == null)
///         return Outcome.NotFound($"Order {id} not found");
///     
///     return Outcome.Success(order);
/// }
/// 
/// // コントローラー層
/// var result = await service.GetOrderByIdAsync(id);
/// return result.ToActionResult(this, order => Ok(order));
/// // → 200 OK (order) または 404 Not Found
/// </code>
/// 
/// <para><strong>パターンマッチング例</strong></para>
/// <code>
/// var message = result.Match(
///     onSuccess: order => $"Found order {order.Id}",
///     onSuccessEmpty: () => "Operation completed",
///     onFailure: error => $"Error: {error}"
/// );
/// </code>
/// </remarks>
public abstract record OperationResult<T>
{
    /// <summary>
    /// 外部からの直接インスタンス化を防止
    /// </summary>
    private OperationResult() { }

    /// <summary>
    /// 値を持つ成功結果
    /// </summary>
    /// <param name="Value">成功時の戻り値</param>
    public sealed record Success(T Value) : OperationResult<T>;

    /// <summary>
    /// 値を持たない成功結果 (例: 削除成功で204 No Contentを返す場合)
    /// </summary>
    /// <remarks>
    /// 主に以下のケースで使用:
    /// <list type="bullet">
    /// <item>DELETE操作の成功 (204 No Content)</item>
    /// <item>PUT操作の成功 (204 No Content)</item>
    /// <item>値を返す必要がない操作全般</item>
    /// </list>
    /// </remarks>
    public sealed record SuccessEmpty : OperationResult<T>;

    /// <summary>
    /// 失敗結果
    /// </summary>
    /// <param name="FailureError">エラー情報</param>
    /// <remarks>
    /// <para><strong>引数名が FailureError である理由</strong></para>
    /// <para>
    /// 親クラスに既に Error プロパティが存在するため、
    /// レコードの引数名を Error にすると名前が衝突し、
    /// コンパイラが初期化の曖昧さを警告します (CS8907)。
    /// FailureError という別名にすることで衝突を回避しています。
    /// </para>
    /// </remarks>
    private sealed record Failure(OperationError FailureError) : OperationResult<T>
    {
        /// <summary>
        /// 親クラスの Error プロパティを明示的に実装
        /// </summary>
        /// <remarks>
        /// new キーワードで親のプロパティを隠蔽し、
        /// FailureError を Error として公開します。
        /// これにより、null 警告 (CS8604) も解消されます。
        /// </remarks>
        public new OperationError Error => FailureError;
    }

    /// <summary>
    /// エラーからの暗黙的変換
    /// </summary>
    /// <remarks>
    /// <code>
    /// // これが可能になる
    /// OperationResult&lt;Order&gt; result = Outcome.NotFound("Order not found");
    /// </code>
    /// </remarks>
    public static implicit operator OperationResult<T>(OperationError error)
        => new Failure(error);


    /// <summary>
    /// パターンマッチングで結果を変換
    /// </summary>
    /// <typeparam name="TResult">変換後の型</typeparam>
    /// <param name="onSuccess">成功時の変換関数</param>
    /// <param name="onSuccessEmpty">成功(値なし)時の変換関数</param>
    /// <param name="onFailure">失敗時の変換関数</param>
    /// <returns>変換後の値</returns>
    /// <remarks>
    /// <example>
    /// <code>
    /// var message = result.Match(
    ///     onSuccess: order => $"Order {order.Id} created",
    ///     onSuccessEmpty: () => "No content",
    ///     onFailure: error => error.GetUserMessage()
    /// );
    /// </code>
    /// </example>
    /// </remarks>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<TResult> onSuccessEmpty,
        Func<OperationError, TResult> onFailure)
    {
        return this switch
        {
            Success s => onSuccess(s.Value),
            SuccessEmpty => onSuccessEmpty(),
            Failure f => onFailure(f.FailureError),
            _ => throw new InvalidOperationException()
        };
    }

    /// <summary>
    /// 成功かどうかを判定
    /// </summary>
    public bool IsSuccess => this is Success or SuccessEmpty;

    /// <summary>
    /// 失敗かどうかを判定
    /// </summary>
    public bool IsFailure => this is Failure;

    /// <summary>
    /// エラー情報を取得（失敗時のみ値を持つ）
    /// </summary>
    public OperationError? Error => this is Failure f ? f.Error : null;

    /// <summary>
    /// ユーザー向けエラーメッセージを取得
    /// </summary>
    /// <remarks>
    /// エラー型に応じて適切なメッセージを生成します。
    /// ログ記録やAPI応答に使用できます。
    /// </remarks>
    public string ErrorMessage => Error switch
    {
        OperationError.NotFound nf => nf.Message ?? "Resource not found",
        OperationError.ValidationFailed vf => $"Validation failed: {string.Join(", ", vf.Errors.Keys)}",
        OperationError.Conflict c => c.Message,
        OperationError.BusinessRule br => $"[{br.Code}] {br.Message}",
        OperationError.Unauthorized u => u.Message,
        OperationError.Forbidden f => f.Message,
        _ => "Unknown error"
    };
}



/// <summary>
/// 値を返さない操作の結果を表現する型
/// </summary>
/// <remarks>
/// <para><strong>設計原則</strong></para>
/// <list type="bullet">
/// <item>Success: 成功（値なし）</item>
/// <item>Failure: エラー情報を持つ失敗</item>
/// </list>
/// 
/// <para><strong>使用例</strong></para>
/// <code>
/// // サービス層
/// public async Task&lt;OperationResult&gt; DeleteProductAsync(int id)
/// {
///     var product = await repository.GetByIdAsync(id);
///     if (product == null)
///         return Outcome.NotFound($"Product {id} not found");
///     
///     await repository.DeleteAsync(id);
///     return Outcome.Success();
/// }
/// 
/// // コントローラー層
/// var result = await service.DeleteProductAsync(id);
/// return result.ToActionResult(this, NoContent);
/// // → 204 No Content または 404 Not Found
/// </code>
/// 
/// <para><strong>OperationResult&lt;T&gt; との使い分け</strong></para>
/// <list type="bullet">
/// <item>値を返す → <c>OperationResult&lt;T&gt;</c></item>
/// <item>値を返さない → <c>OperationResult</c></item>
/// </list>
/// </remarks>
public abstract record OperationResult
{
    /// <summary>
    /// 外部からの直接インスタンス化を防止
    /// </summary>
    private OperationResult() { }

    /// <summary>
    /// 成功結果
    /// </summary>
    public sealed record Success : OperationResult;

    /// <summary>
    /// 失敗結果
    /// </summary>
    /// <param name="FailureError">エラー情報</param>
    /// <remarks>
    /// 引数名が FailureError である理由は OperationResult&lt;T&gt; の説明を参照。
    /// </remarks>
    private sealed record Failure(OperationError FailureError) : OperationResult
    {
        /// <summary>
        /// 親クラスの Error プロパティを明示的に実装
        /// </summary>
        public new OperationError Error => FailureError;
    }

    /// <summary>
    /// エラーからの暗黙的変換
    /// </summary>
    public static implicit operator OperationResult(OperationError error)
        => new Failure(error);

    /// <summary>
    /// パターンマッチングで結果を変換
    /// </summary>
    /// <param name="onSuccess">成功時の変換関数</param>
    /// <param name="onFailure">失敗時の変換関数</param>
    /// <typeparam name="TResult">変換後の型</typeparam>
    /// <returns>変換後の値</returns>
    /// <remarks>
    /// <example>
    /// <code>
    /// var message = result.Match(
    ///     onSuccess: () => "No content",
    ///     onFailure: error => error.GetUserMessage()
    /// );
    /// </code>
    /// </example>
    /// </remarks>
    public TResult Match<TResult>(
        Func<TResult> onSuccess,
        Func<OperationError, TResult> onFailure)
    {
        return this switch
        {
            Success => onSuccess(),
            Failure f => onFailure(f.FailureError),
            _ => throw new InvalidOperationException()
        };
    }

    /// <summary>
    /// 成功かどうかを判定
    /// </summary>
    public bool IsSuccess => this is Success;

    /// <summary>
    /// 失敗かどうかを判定
    /// </summary>
    public bool IsFailure => this is Failure;

    /// <summary>
    /// エラー情報を取得（失敗時のみ値を持つ）
    /// </summary>
    public OperationError? Error => this is Failure f ? f.Error : null;

    /// <summary>
    /// ユーザー向けエラーメッセージを取得
    /// </summary>
    public string ErrorMessage => Error switch
    {
        OperationError.NotFound nf => nf.Message ?? "Resource not found",
        OperationError.ValidationFailed vf => $"Validation failed: {string.Join(", ", vf.Errors.Keys)}",
        OperationError.Conflict c => c.Message,
        OperationError.BusinessRule br => $"[{br.Code}] {br.Message}",
        OperationError.Unauthorized u => u.Message,
        OperationError.Forbidden f => f.Message,
        _ => "Unknown error"
    };
}
