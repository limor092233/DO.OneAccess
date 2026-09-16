using DO.OneAccess.Application.DTOs.Auth;

namespace DO.OneAccess.Client.Services;

public interface IClientAuthService
{
    event Action? AuthenticationStateChanged;
    string? GetAccessToken();
    Task<bool> LoginAsync(LoginRequestDto request);
    Task<bool> RefreshAsync();
    Task LogoutAsync();
}
