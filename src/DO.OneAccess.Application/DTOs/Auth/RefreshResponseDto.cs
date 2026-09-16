namespace DO.OneAccess.Application.DTOs.Auth;

public class RefreshResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}
