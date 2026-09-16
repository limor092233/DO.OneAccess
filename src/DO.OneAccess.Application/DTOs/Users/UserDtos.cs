namespace DO.OneAccess.Application.DTOs.Users;

public class UserDto
{
    public Guid UserId { get; init; }
    public string EmployeeNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Position { get; init; }
    public int? SectionId { get; init; }
    public string? SectionName { get; init; }
    public int? DivisionId { get; init; }
    public string? DivisionName { get; init; }
    public short RoleId { get; init; }
    public string RoleCode { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class CreateUserDto
{
    public string EmployeeNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Position { get; init; }
    public int? SectionId { get; init; }
    public int? DivisionId { get; init; }
    public short RoleId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public class TransferSystemAdministratorDto
{
    public Guid TargetUserId { get; init; }
    public short PreviousAdminNewRoleId { get; init; }
    public int? PreviousAdminSectionId { get; init; }
    public int? PreviousAdminDivisionId { get; init; }
}

public class UpdateUserDto
{
    public string? FirstName { get; init; }
    public string? MiddleName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? Position { get; init; }
    public int? SectionId { get; init; }
    public string? Username { get; init; }
    public bool? IsActive { get; init; }
}

public class ChangeRoleDto
{
    public short RoleId { get; init; }
}

public class UserQueryDto
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool? IsActive { get; init; }
    public int? SectionId { get; init; }
    public string? UsernameContains { get; init; }
    public string? SearchTerm { get; init; }
}
