using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Infrastructure.Security;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class JwtTokenServiceTests
{
    private readonly IConfiguration _config;
    private const string ValidSecret = "ThisIsASecretKeyWithMoreThan32BytesLength!";

    public JwtTokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Issuer", "DO.OneAccess" },
            { "Jwt:Audience", "DO.OneAccess.API" },
            { "Jwt:AccessTokenLifetimeMinutes", "15" },
            { "Jwt:RefreshTokenLifetimeDays", "7" },
            { "Jwt:Key", ValidSecret }
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainRequiredClaimsAndValidSignature()
    {
        var service = new JwtTokenService(_config);
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = "employee_admin"
        };
        var roleCode = "SYSTEM_ADMINISTRATOR";

        var tokenString = service.GenerateAccessToken(user, roleCode);
        Assert.False(string.IsNullOrWhiteSpace(tokenString));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal("DO.OneAccess", token.Issuer);
        Assert.Contains("DO.OneAccess.API", token.Audiences);

        // Claims check
        var subClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.NameIdentifier);
        Assert.NotNull(subClaim);
        Assert.Equal(user.UserId.ToString(), subClaim.Value);

        var nameClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName || c.Type == ClaimTypes.Name);
        Assert.NotNull(nameClaim);
        Assert.Equal("employee_admin", nameClaim.Value);

        var roleClaim = token.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal("SYSTEM_ADMINISTRATOR", roleClaim.Value);

        var jtiClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.NotNull(jtiClaim);
        Assert.True(Guid.TryParse(jtiClaim.Value, out _));

        // Expiration check (approx 15 min)
        var diff = token.ValidTo - DateTime.UtcNow;
        Assert.True(diff.TotalMinutes > 14 && diff.TotalMinutes <= 16);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnHighEntropyTokenAndValidSha256Hash()
    {
        var service = new JwtTokenService(_config);
        var (rawToken, tokenHash, expiresAt) = service.GenerateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(rawToken));
        Assert.False(string.IsNullOrWhiteSpace(tokenHash));
        Assert.NotEqual(rawToken, tokenHash);

        // Hash length for SHA-256 hex string is exactly 64 characters
        Assert.Equal(64, tokenHash.Length);

        // Expiration is approx 7 days
        var diff = expiresAt - DateTime.UtcNow;
        Assert.True(diff.TotalDays > 6.9 && diff.TotalDays <= 7.1);

        // Verifying hash is deterministic
        var recomputedHash = service.HashRefreshToken(rawToken);
        Assert.Equal(tokenHash, recomputedHash);
    }

    [Fact]
    public void Constructor_WithKeyShorterThan32Bytes_ShouldThrowInvalidOperationException()
    {
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Key", "ShortKey123" }
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(invalidConfig));
    }
}
