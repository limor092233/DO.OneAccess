namespace DO.OneAccess.Application.DTOs.Systems;

public class SystemDto
{
    public Guid SystemId { get; init; }
    public string SystemCode { get; init; } = string.Empty;
    public string SystemName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? BaseUrl { get; init; }
    public string? IconUrl { get; init; }
    public string DefaultAccess { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class RegisterSystemDto
{
    public string SystemCode { get; init; } = string.Empty;
    public string SystemName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? BaseUrl { get; init; }
    public string? IconUrl { get; init; }
    public string DefaultAccess { get; init; } = "Restricted";
}

public class UpdateSystemDto
{
    public string? SystemName { get; init; }
    public string? Description { get; init; }
    public string? BaseUrl { get; init; }
    public string? IconUrl { get; init; }
    public string? DefaultAccess { get; init; }
    public bool? IsActive { get; init; }
}
