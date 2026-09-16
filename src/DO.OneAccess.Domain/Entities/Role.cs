namespace DO.OneAccess.Domain.Entities;

public class Role
{
    public short RoleId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
}
