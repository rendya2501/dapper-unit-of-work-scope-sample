namespace Domain.Entities;

/// <summary>
/// 監査ログエンティティ
/// </summary>
public class AuditLog
{
    /// <summary>
    /// ログID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// アクション種別
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 詳細情報
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// 記録日時
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
