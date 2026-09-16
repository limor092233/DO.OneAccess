namespace DO.OneAccess.Domain.Entities;

public class User
{
    public Guid UserId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Position { get; set; }
    public int? SectionId { get; set; }
    public short RoleId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public Section? Section { get; set; }
    public Role Role { get; set; } = null!;
    public ICollection<UserSystemAccess> UserSystemAccesses { get; set; } = new List<UserSystemAccess>();
    public AdministratorScope? AdministratorScope { get; set; }
    public ICollection<AdministratorSystemAccess> AdministratorSystemAccesses { get; set; } = new List<AdministratorSystemAccess>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<LoginHistory> LoginHistories { get; set; } = new List<LoginHistory>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
