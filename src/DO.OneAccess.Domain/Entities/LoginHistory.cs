namespace DO.OneAccess.Domain.Entities;

public class LoginHistory
{
    public long LoginHistoryId { get; set; }
    public Guid? UserId { get; set; }
    public string? UsernameAttempted { get; set; }
    public bool Success { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
