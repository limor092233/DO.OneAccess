namespace DO.OneAccess.Application.DTOs.Access;

/// <summary>Internal result of user system access resolution. Not API-facing.</summary>
public record SystemAccessResult(bool IsGranted, string Reason);

public class UserSystemAccessDto
{
    public long UserSystemAccessId { get; init; }
    public Guid UserId { get; init; }
    public Guid SystemId { get; init; }
    public string SystemName { get; init; } = string.Empty;
    public string AccessType { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public Guid? UpdatedBy { get; init; }
}

public class SetUserSystemAccessDto
{
    public Guid UserId { get; init; }
    public Guid SystemId { get; init; }
    public string AccessType { get; init; } = string.Empty;
}

public class AdminScopeDto
{
    public long AdministratorScopeId { get; init; }
    public Guid AdministratorUserId { get; init; }
    public int DivisionId { get; init; }
    public string DivisionName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
}

public class AssignAdminScopeDto
{
    public Guid AdministratorUserId { get; init; }
    public int DivisionId { get; init; }
}

public class AdminSystemAccessDto
{
    public long AdministratorSystemAccessId { get; init; }
    public Guid AdministratorUserId { get; init; }
    public Guid SystemId { get; init; }
    public string SystemName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
}

public class GrantAdminSystemAccessDto
{
    public Guid AdministratorUserId { get; init; }
    public Guid SystemId { get; init; }
}
