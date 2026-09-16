namespace DO.OneAccess.Domain.Entities;

public class AdministratorSystemAccess
{
    public long AdministratorSystemAccessId { get; set; }
    public global::System.Guid AdministratorUserId { get; set; }
    public global::System.Guid SystemId { get; set; }
    public global::System.DateTime CreatedAt { get; set; }
    public global::System.Guid? CreatedBy { get; set; }
    public global::System.DateTime? UpdatedAt { get; set; }
    public global::System.Guid? UpdatedBy { get; set; }

    // Navigation properties
    public User AdministratorUser { get; set; } = null!;
    public System System { get; set; } = null!;
}
