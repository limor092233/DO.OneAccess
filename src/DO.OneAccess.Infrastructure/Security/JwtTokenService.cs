using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly byte[] _signingKey;
    private readonly int _accessTokenLifetimeMinutes;
    private readonly int _refreshTokenLifetimeDays;

    public JwtTokenService(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"] ?? "DO.OneAccess";
        _audience = configuration["Jwt:Audience"] ?? "DO.OneAccess.API";
        
        var key = configuration["Jwt:Key"] 
            ?? throw new InvalidOperationException("Configuration 'Jwt:Key' is required and cannot be null.");
        
        _signingKey = Encoding.UTF8.GetBytes(key);
        if (_signingKey.Length < 32)
        {
            throw new InvalidOperationException("Configuration 'Jwt:Key' must be at least 256 bits (32 bytes).");
        }

        if (!int.TryParse(configuration["Jwt:AccessTokenLifetimeMinutes"], out _accessTokenLifetimeMinutes) || _accessTokenLifetimeMinutes <= 0)
        {
            _accessTokenLifetimeMinutes = 15;
        }

        if (!int.TryParse(configuration["Jwt:RefreshTokenLifetimeDays"], out _refreshTokenLifetimeDays) || _refreshTokenLifetimeDays <= 0)
        {
            _refreshTokenLifetimeDays = 7;
        }
    }

    public int AccessTokenLifetimeSeconds => _accessTokenLifetimeMinutes * 60;

    public string GenerateAccessToken(User user, string roleCode)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var expires = DateTime.UtcNow.AddMinutes(_accessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, roleCode),
            new("role", roleCode),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_signingKey), 
                SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public (string RawToken, string TokenHash, DateTime ExpiresAt) GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var tokenHash = HashRefreshToken(rawToken);
        var expiresAt = DateTime.UtcNow.AddDays(_refreshTokenLifetimeDays);

        return (rawToken, tokenHash, expiresAt);
    }

    public string HashRefreshToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
