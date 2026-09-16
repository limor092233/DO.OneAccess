namespace DO.OneAccess.Domain.Entities;

public class UserSystemAccess
{
    public long UserSystemAccessId { get; set; }
    public global::System.Guid UserId { get; set; }
    public global::System.Guid SystemId { get; set; }
    public string AccessType { get; set; } = string.Empty;
    public global::System.DateTime CreatedAt { get; set; }
    public global::System.Guid? CreatedBy { get; set; }
    public global::System.DateTime? UpdatedAt { get; set; }
    public global::System.Guid? UpdatedBy { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public System System { get; set; } = null!;
}
