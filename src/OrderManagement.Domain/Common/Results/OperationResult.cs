using System.Diagnostics.CodeAnalysis;

namespace Domain.Common.Results;

/// <summary>
/// 操作結果を表現する基底型（classベース）
/// </summary>
/// <remarks>
/// <para><strong>設計原則</strong></para>
/// <list type="bullet">
/// <item>classベース（recordの複雑さを排除）</item>
/// <item>フラグで成功/失敗を判定（型判別不要）</item>
/// <item>概念と実装が完全に一致</item>
/// <item>シンプルで理解しやすい</item>
/// </list>
/// 
/// <para><strong>使用例</strong></para>
/// <code>
/// // 値なし操作
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
/// // 値あり操作
/// public async Task&lt;OperationResult&lt;Order&gt;&gt; GetOrderAsync(int id)
/// {
///     var order = await repository.GetByIdAsync(id);
///     if (order == null)
///         return Outcome.NotFound($"Order {id} not found");
///     
///     return Outcome.Success(order);
/// }
/// </code>
/// </remarks>
public class OperationResult
{
    protected OperationResult(bool isSuccess, OperationError? error)
    {
        // 不正な組み合わせをチェック
        if (isSuccess && error != null)
            throw new ArgumentException("Success result cannot have an error", nameof(error));
        if (!isSuccess && error == null)
            throw new ArgumentException("Failure result must have an error", nameof(error));

        IsSuccess = isSuccess;
        Error = error;
    }

    // ===== プロパティ =====

    /// <summary>
    /// 成功かどうかを判定
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 失敗かどうかを判定
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// エラー情報を取得（失敗時のみ値を持つ）
    /// </summary>
    public OperationError? Error { get; }

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
        null => string.Empty,
        _ => "Unknown error"
    };

    // ===== ファクトリメソッド =====

    /// <summary>
    /// 値なし成功を生成
    /// </summary>
    public static OperationResult Success() => new(true, null);

    /// <summary>
    /// 値あり成功を生成
    /// </summary>
    public static OperationResult<T> Success<T>(T value) => new(value, true, null);

    /// <summary>
    /// 値なし失敗を生成
    /// </summary>
    public static OperationResult Failure(OperationError error) => new(false, error);

    /// <summary>
    /// 値あり失敗を生成
    /// </summary>
    /// <remarks>
    /// 型推論が効かない場合のみ使用。
    /// 通常は Failure(error) で十分（戻り値の型から推論される）。
    /// </remarks>
    public static OperationResult<T> Failure<T>(OperationError error) => new(default, false, error);

    // ===== 暗黙的変換 =====

    /// <summary>
    /// エラーからの暗黙的変換
    /// </summary>
    public static implicit operator OperationResult(OperationError error) => Failure(error);
}


/// <summary>
/// 値を返す操作の結果
/// </summary>
/// <typeparam name="T">成功時に返す値の型</typeparam>
public class OperationResult<T>(T? value, bool isSuccess, OperationError? error) : OperationResult(isSuccess, error)
{
    /// <summary>
    /// 成功時の値を取得
    /// </summary>
    /// <exception cref="InvalidOperationException">失敗結果で値にアクセスした場合</exception>
    [NotNull]
    public T Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("Cannot access value of a failure result");

    /// <summary>
    /// エラーからの暗黙的変換
    /// </summary>
    public static implicit operator OperationResult<T>(OperationError error) => Failure<T>(error);

    /// <summary>
    /// 値からの暗黙的変換
    /// </summary>
    public static implicit operator OperationResult<T>(T value) => Success(value);
}


///// <summary>
///// 値を返す操作の結果
///// </summary>
///// <typeparam name="T">成功時に返す値の型</typeparam>
//public class OperationResult<T> : OperationResult
//{
//    private readonly T? _value;

//    internal OperationResult(T? value, bool isSuccess, OperationError? error)
//        : base(isSuccess, error)
//    {
//        _value = value;
//    }

//    /// <summary>
//    /// 成功時の値を取得
//    /// </summary>
//    /// <exception cref="InvalidOperationException">失敗結果で値にアクセスした場合</exception>
//    [NotNull]
//    public T Value => IsSuccess
//        ? _value!
//        : throw new InvalidOperationException("Cannot access value of a failure result");

//    // ===== 暗黙的変換 =====

//    /// <summary>
//    /// エラーからの暗黙的変換
//    /// </summary>
//    public static implicit operator OperationResult<T>(OperationError error) => Failure<T>(error);

//    /// <summary>
//    /// 値からの暗黙的変換
//    /// </summary>
//    public static implicit operator OperationResult<T>(T value) => Success(value);
//}
