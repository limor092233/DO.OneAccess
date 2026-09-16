namespace DO.OneAccess.Domain.Entities;

public class AuditLog
{
    public long AuditLogId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
