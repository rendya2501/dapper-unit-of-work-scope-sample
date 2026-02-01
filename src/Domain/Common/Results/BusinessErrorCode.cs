namespace Domain.Common.Results;

/// <summary>
/// ビジネスルール違反のエラーコード定義
/// </summary>
/// <remarks>
/// <para><strong>命名規則</strong></para>
/// <list type="bullet">
/// <item>PascalCase で定義（C# enum標準）</item>
/// <item>ToErrorCode() 拡張メソッドで UPPER_SNAKE_CASE に変換</item>
/// <item>フロントエンドと共有する場合は OpenAPI 経由で型定義を生成</item>
/// </list>
/// 
/// <para><strong>使用例</strong></para>
/// <code>
/// // サービス層
/// if (product.Stock &lt; quantity)
///     return Outcome.BusinessRule(
///         BusinessErrorCode.InsufficientStock.ToErrorCode(),
///         $"Available: {product.Stock}, Requested: {quantity}");
/// 
/// // フロントエンド (TypeScript)
/// if (error.code === 'INSUFFICIENT_STOCK') {
///     showNotification('在庫不足です');
/// }
/// </code>
/// 
/// <para><strong>カテゴリ分類</strong></para>
/// <list type="bullet">
/// <item>在庫関連: InsufficientStock, StockReservationFailed</item>
/// <item>注文関連: EmptyOrder, OrderExpired, OrderLimitExceeded</item>
/// <item>支払い関連: PaymentFailed, InvalidPaymentMethod</item>
/// <item>配送関連: InvalidShippingAddress, ShippingUnavailable</item>
/// <item>その他: InvalidCoupon, ValidationFailed</item>
/// </list>
/// </remarks>
public enum BusinessErrorCode
{
    // ========================================
    // 在庫関連エラー
    // ========================================

    /// <summary>
    /// 在庫不足
    /// </summary>
    /// <remarks>
    /// 商品の在庫数が注文数量より少ない場合。
    /// フロントエンドでは「在庫不足通知」を表示し、数量調整を促す。
    /// </remarks>
    InsufficientStock,

    /// <summary>
    /// 在庫予約失敗
    /// </summary>
    /// <remarks>
    /// 在庫の一時確保（予約）に失敗した場合。
    /// 同時アクセスによる競合などで発生する可能性がある。
    /// </remarks>
    StockReservationFailed,

    /// <summary>
    /// 無効な在庫移動
    /// </summary>
    /// <remarks>
    /// 倉庫間移動や在庫調整が不正な場合。
    /// </remarks>
    InvalidStockMovement,

    // ========================================
    // 注文関連エラー
    // ========================================

    /// <summary>
    /// 空の注文
    /// </summary>
    /// <remarks>
    /// 注文アイテムが1件も含まれていない場合。
    /// </remarks>
    EmptyOrder,

    /// <summary>
    /// 無効な数量
    /// </summary>
    /// <remarks>
    /// 注文数量が0以下、または上限を超えている場合。
    /// </remarks>
    InvalidQuantity,

    /// <summary>
    /// 注文期限切れ
    /// </summary>
    /// <remarks>
    /// カート内の商品が一定時間経過して無効になった場合。
    /// </remarks>
    OrderExpired,

    /// <summary>
    /// 注文上限超過
    /// </summary>
    /// <remarks>
    /// 1回の注文で許可される商品数・金額の上限を超えた場合。
    /// </remarks>
    OrderLimitExceeded,

    /// <summary>
    /// 注文のキャンセル不可
    /// </summary>
    /// <remarks>
    /// 既に発送済みなど、キャンセルできない状態の注文をキャンセルしようとした場合。
    /// </remarks>
    OrderNotCancellable,

    // ========================================
    // 支払い関連エラー
    // ========================================

    /// <summary>
    /// 支払い失敗
    /// </summary>
    /// <remarks>
    /// クレジットカード決済、電子マネー決済などが失敗した場合。
    /// </remarks>
    PaymentFailed,

    /// <summary>
    /// 無効な支払い方法
    /// </summary>
    /// <remarks>
    /// 選択された支払い方法が利用できない、または無効な場合。
    /// </remarks>
    InvalidPaymentMethod,

    /// <summary>
    /// 残高不足
    /// </summary>
    /// <remarks>
    /// ポイント払い、ウォレット払いなどで残高が不足している場合。
    /// </remarks>
    InsufficientFunds,

    // ========================================
    // 配送関連エラー
    // ========================================

    /// <summary>
    /// 無効な配送先住所
    /// </summary>
    /// <remarks>
    /// 配送先の住所が不完全、または配送不可地域の場合。
    /// </remarks>
    InvalidShippingAddress,

    /// <summary>
    /// 配送不可
    /// </summary>
    /// <remarks>
    /// 商品の性質上、指定の住所への配送ができない場合。
    /// （例: 冷凍商品が離島配送不可など）
    /// </remarks>
    ShippingUnavailable,

    // ========================================
    // クーポン・キャンペーン関連エラー
    // ========================================

    /// <summary>
    /// 無効なクーポン
    /// </summary>
    /// <remarks>
    /// クーポンコードが存在しない、期限切れ、使用済み、条件未達などの場合。
    /// </remarks>
    InvalidCoupon,

    /// <summary>
    /// クーポン適用条件未達
    /// </summary>
    /// <remarks>
    /// クーポンの適用条件（最低金額、対象商品など）を満たしていない場合。
    /// </remarks>
    CouponConditionNotMet,

    // ========================================
    // その他のエラー
    // ========================================

    /// <summary>
    /// 汎用的なバリデーション失敗
    /// </summary>
    /// <remarks>
    /// 複数のバリデーションエラーをまとめて表現する場合に使用。
    /// 通常は OperationError.ValidationFailed を使用することを推奨。
    /// </remarks>
    ValidationFailed,

    /// <summary>
    /// 操作がタイムアウト
    /// </summary>
    /// <remarks>
    /// 外部APIとの通信など、操作が時間内に完了しなかった場合。
    /// </remarks>
    OperationTimeout
}


/// <summary>
/// BusinessErrorCode のヘルパー拡張メソッド
/// </summary>
public static class BusinessErrorCodeExtensions
{
    /// <summary>
    /// enum値を UPPER_SNAKE_CASE の文字列に変換
    /// </summary>
    /// <example>
    /// <code>
    /// BusinessErrorCode.InsufficientStock.ToErrorCode() // → "INSUFFICIENT_STOCK"
    /// </code>
    /// </example>
    public static string ToErrorCode(this BusinessErrorCode code)
    {
        return string.Concat(
            code.ToString()
                .Select((c, i) => i > 0 && char.IsUpper(c) ? $"_{c}" : c.ToString())
        ).ToUpperInvariant();
    }

    /// <summary>
    /// エラーコードのユーザー向けメッセージを取得
    /// </summary>
    /// <param name="code">ビジネスエラーコード</param>
    /// <returns>日本語のエラーメッセージ</returns>
    /// <remarks>
    /// 多言語対応が必要な場合は、リソースファイル（.resx）を使用することを推奨。
    /// </remarks>
    /// <example>
    /// <code>
    /// var message = BusinessErrorCode.InsufficientStock.GetUserMessage();
    /// // → "在庫が不足しています"
    /// </code>
    /// </example>
    public static string GetUserMessage(this BusinessErrorCode code)
    {
        return code switch
        {
            BusinessErrorCode.InsufficientStock => "在庫が不足しています",
            BusinessErrorCode.StockReservationFailed => "在庫の確保に失敗しました",
            BusinessErrorCode.InvalidStockMovement => "在庫移動が無効です",
            BusinessErrorCode.EmptyOrder => "注文にアイテムが含まれていません",
            BusinessErrorCode.InvalidQuantity => "注文数量が無効です",
            BusinessErrorCode.OrderExpired => "注文の有効期限が切れています",
            BusinessErrorCode.OrderLimitExceeded => "注文上限を超えています",
            BusinessErrorCode.OrderNotCancellable => "この注文はキャンセルできません",
            BusinessErrorCode.PaymentFailed => "支払い処理に失敗しました",
            BusinessErrorCode.InvalidPaymentMethod => "無効な支払い方法です",
            BusinessErrorCode.InsufficientFunds => "残高が不足しています",
            BusinessErrorCode.InvalidShippingAddress => "配送先住所が無効です",
            BusinessErrorCode.ShippingUnavailable => "この商品は配送できません",
            BusinessErrorCode.InvalidCoupon => "クーポンが無効です",
            BusinessErrorCode.CouponConditionNotMet => "クーポンの適用条件を満たしていません",
            BusinessErrorCode.ValidationFailed => "入力内容に誤りがあります",
            BusinessErrorCode.OperationTimeout => "処理がタイムアウトしました",
            _ => "エラーが発生しました"
        };
    }
}
