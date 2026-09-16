namespace DO.OneAccess.Application.DTOs.Divisions;

public class DivisionDto
{
    public int DivisionId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class CreateDivisionDto
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public class UpdateDivisionDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }
}
