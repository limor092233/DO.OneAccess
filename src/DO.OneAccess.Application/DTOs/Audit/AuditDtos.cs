namespace DO.OneAccess.Application.DTOs.Audit;

public class AuditLogDto
{
    public long AuditLogId { get; init; }
    public Guid? UserId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? EntityName { get; init; }
    public string? EntityId { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? IpAddress { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class WriteAuditLogDto
{
    public Guid? UserId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? EntityName { get; init; }
    public string? EntityId { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? IpAddress { get; init; }
}

public class AuditLogQueryDto
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public Guid? UserId { get; init; }
    public string? Action { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

public class LoginHistoryDto
{
    public long LoginHistoryId { get; init; }
    public Guid? UserId { get; init; }
    public string? UsernameAttempted { get; init; }
    public bool Success { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? FailureReason { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class LoginHistoryQueryDto
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public Guid? UserId { get; init; }
    public bool? Success { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}
