namespace DO.OneAccess.Domain.Entities;

public class AdministratorScope
{
    public long AdministratorScopeId { get; set; }
    public Guid AdministratorUserId { get; set; }
    public int DivisionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public User AdministratorUser { get; set; } = null!;
    public Division Division { get; set; } = null!;
}
