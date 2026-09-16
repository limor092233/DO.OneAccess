namespace DO.OneAccess.Domain.Entities;

public class Division
{
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
    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<AdministratorScope> AdministratorScopes { get; set; } = new List<AdministratorScope>();
}
