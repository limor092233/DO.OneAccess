namespace DO.OneAccess.Application.DTOs.Auth;

/// <summary>
/// Internal result object used between Application AuthService and Server AuthController.
/// The RawRefreshToken is extracted by the controller to set the HttpOnly cookie,
/// and is never serialized into the client JSON response.
/// </summary>
public class AuthTokenResult
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public string RawRefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
}
