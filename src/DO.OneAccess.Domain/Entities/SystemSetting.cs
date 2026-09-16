namespace DO.OneAccess.Domain.Entities;

public class SystemSetting
{
    public long SystemSettingId { get; set; }
    public global::System.Guid SystemId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string? SettingValue { get; set; }
    public bool IsSecret { get; set; }
    public global::System.DateTime CreatedAt { get; set; }
    public global::System.Guid? CreatedBy { get; set; }
    public global::System.DateTime? UpdatedAt { get; set; }
    public global::System.Guid? UpdatedBy { get; set; }

    // Navigation properties
    public System System { get; set; } = null!;
}
