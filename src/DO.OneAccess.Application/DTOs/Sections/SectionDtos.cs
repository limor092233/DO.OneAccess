namespace DO.OneAccess.Application.DTOs.Sections;

public class SectionDto
{
    public int SectionId { get; init; }
    public int DivisionId { get; init; }
    public string DivisionName { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class CreateSectionDto
{
    public int DivisionId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public class UpdateSectionDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }
}

public class SectionQueryDto
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public bool? IsActive { get; init; }
    public int? DivisionId { get; init; }
}
