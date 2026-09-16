namespace DO.OneAccess.Domain.Entities;

public class Section
{
    public int SectionId { get; set; }
    public int DivisionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public Division Division { get; set; } = null!;
    public ICollection<User> Users { get; set; } = new List<User>();
}
