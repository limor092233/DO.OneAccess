namespace DO.OneAccess.Application.DTOs.Setup;

public class SetupStatusDto
{
    public bool IsInitialized { get; init; }
}

public class FirstRunSetupDto
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class FirstRunSetupResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
}
