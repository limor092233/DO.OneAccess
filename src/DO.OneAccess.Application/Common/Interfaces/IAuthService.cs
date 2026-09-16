using DO.OneAccess.Application.DTOs.Auth;

namespace DO.OneAccess.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthTokenResult> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<AuthTokenResult> RefreshTokenAsync(string rawRefreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(string? rawRefreshToken, string? ipAddress, CancellationToken cancellationToken = default);
}
