namespace DO.OneAccess.Domain.Entities;

public class System
{
    public global::System.Guid SystemId { get; set; }
    public string SystemCode { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BaseUrl { get; set; }
    public string? IconUrl { get; set; }
    public string DefaultAccess { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public global::System.DateTime CreatedAt { get; set; }
    public global::System.Guid? CreatedBy { get; set; }
    public global::System.DateTime? UpdatedAt { get; set; }
    public global::System.Guid? UpdatedBy { get; set; }

    // Navigation properties
    public ICollection<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();
    public ICollection<UserSystemAccess> UserSystemAccesses { get; set; } = new List<UserSystemAccess>();
    public ICollection<AdministratorSystemAccess> AdministratorSystemAccesses { get; set; } = new List<AdministratorSystemAccess>();
}
