using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, string roleCode);
    (string RawToken, string TokenHash, DateTime ExpiresAt) GenerateRefreshToken();
    string HashRefreshToken(string rawToken);
    int AccessTokenLifetimeSeconds { get; }
}
