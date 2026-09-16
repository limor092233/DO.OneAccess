using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.DTOs.Auth;
using DO.OneAccess.Application.Services;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Infrastructure.Persistence;
using DO.OneAccess.Infrastructure.Security;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;
    private readonly AuthService _authService;
    private readonly Guid _userId = Guid.NewGuid();

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "AuthServiceTestDb_" + Guid.NewGuid())
            .Options;

        _context = new AppDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Issuer", "DO.OneAccess" },
                { "Jwt:Audience", "DO.OneAccess.API" },
                { "Jwt:AccessTokenLifetimeMinutes", "15" },
                { "Jwt:RefreshTokenLifetimeDays", "7" },
                { "Jwt:Key", "ThisIsASecretKeyWithMoreThan32BytesLength!" }
            })
            .Build();

        _passwordHasher = new PasswordHasher();
        _jwtTokenService = new JwtTokenService(config);
        _authService = new AuthService(_context, _passwordHasher, _jwtTokenService);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        var role = new Role
        {
            RoleId = 1,
            Code = "SYSTEM_ADMINISTRATOR",
            Name = "System Administrator",
            IsActive = true
        };
        _context.Roles.Add(role);

        var division = new Division
        {
            DivisionId = 1,
            Code = "EXEC",
            Name = "Executive",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Divisions.Add(division);

        var section = new Section
        {
            SectionId = 1,
            DivisionId = 1,
            Code = "IT",
            Name = "IT Systems",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Sections.Add(section);

        var user = new User
        {
            UserId = _userId,
            EmployeeNumber = "EMP001",
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            Position = "Admin",
            SectionId = null,
            RoleId = 1,
            Username = "alicesmith",
            PasswordHash = _passwordHasher.HashPassword(new User(), "SecureP@ssword123!"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        _context.SaveChanges();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldSucceedAndRecordLoginHistory()
    {
        var request = new LoginRequestDto
        {
            Username = "alicesmith",
            Password = "SecureP@ssword123!"
        };

        var result = await _authService.LoginAsync(request, "127.0.0.1", "UnitTestAgent");

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RawRefreshToken));
        Assert.Equal(900, result.ExpiresIn);

        // Verify User state
        var user = await _context.Users.FindAsync(_userId);
        Assert.NotNull(user!.LastLoginAt);

        // Verify RefreshToken in DB
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == _userId);
        Assert.NotNull(token);
        Assert.Equal(_jwtTokenService.HashRefreshToken(result.RawRefreshToken), token.TokenHash);

        // Verify LoginHistory in DB
        var history = await _context.LoginHistories.FirstOrDefaultAsync(h => h.UserId == _userId);
        Assert.NotNull(history);
        Assert.True(history.Success);
        Assert.Equal("alicesmith", history.UsernameAttempted);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowAndRecordFailure()
    {
        var request = new LoginRequestDto
        {
            Username = "alicesmith",
            Password = "WrongPassword999!"
        };

        await Assert.ThrowsAsync<AuthenticationException>(() =>
            _authService.LoginAsync(request, "127.0.0.1", "UnitTestAgent"));

        var history = await _context.LoginHistories.FirstOrDefaultAsync(h => h.UsernameAttempted == "alicesmith");
        Assert.NotNull(history);
        Assert.False(history.Success);
        Assert.Equal("Invalid credentials", history.FailureReason);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ShouldThrowAndRecordFailureWithoutRevealingAccount()
    {
        var request = new LoginRequestDto
        {
            Username = "ghostuser",
            Password = "Password123!"
        };

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() =>
            _authService.LoginAsync(request, "127.0.0.1", "UnitTestAgent"));

        Assert.Equal("Invalid username or password.", ex.Message);

        var history = await _context.LoginHistories.FirstOrDefaultAsync(h => h.UsernameAttempted == "ghostuser");
        Assert.NotNull(history);
        Assert.False(history.Success);
        Assert.Null(history.UserId);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ShouldThrowAndRecordFailure()
    {
        var user = await _context.Users.FindAsync(_userId);
        user!.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            Username = "alicesmith",
            Password = "SecureP@ssword123!"
        };

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() =>
            _authService.LoginAsync(request, "127.0.0.1", "UnitTestAgent"));

        Assert.Equal("Invalid username or password.", ex.Message);

        var history = await _context.LoginHistories.OrderByDescending(h => h.CreatedAt).FirstOrDefaultAsync();
        Assert.NotNull(history);
        Assert.False(history.Success);
        Assert.Equal("User inactive", history.FailureReason);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldRotateTokenAndLinkOldToken()
    {
        // 1. First login
        var loginResult = await _authService.LoginAsync(new LoginRequestDto
        {
            Username = "alicesmith",
            Password = "SecureP@ssword123!"
        }, "127.0.0.1", "UnitTestAgent");

        // 2. Refresh
        var refreshResult = await _authService.RefreshTokenAsync(loginResult.RawRefreshToken, "127.0.0.1", "UnitTestAgent");

        Assert.NotNull(refreshResult);
        Assert.NotEqual(loginResult.RawRefreshToken, refreshResult.RawRefreshToken);
        Assert.NotEqual(loginResult.AccessToken, refreshResult.AccessToken);

        // 3. Verify old token is revoked and points to new token
        var oldTokenHash = _jwtTokenService.HashRefreshToken(loginResult.RawRefreshToken);
        var oldToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == oldTokenHash);
        Assert.NotNull(oldToken);
        Assert.NotNull(oldToken!.RevokedAt);
        Assert.NotNull(oldToken.ReplacedByTokenId);

        // 4. Verify new token exists and is active
        var newTokenHash = _jwtTokenService.HashRefreshToken(refreshResult.RawRefreshToken);
        var newToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == newTokenHash);
        Assert.NotNull(newToken);
        Assert.Equal(newToken!.RefreshTokenId, oldToken.ReplacedByTokenId);
        Assert.Null(newToken.RevokedAt);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenReplayed_ShouldRevokeAllUserTokensAndLogSecurityAlert()
    {
        // 1. Login
        var loginResult = await _authService.LoginAsync(new LoginRequestDto
        {
            Username = "alicesmith",
            Password = "SecureP@ssword123!"
        }, "127.0.0.1", "UnitTestAgent");

        // 2. Legitimate refresh (old token is rotated)
        var refreshResult = await _authService.RefreshTokenAsync(loginResult.RawRefreshToken, "127.0.0.1", "UnitTestAgent");

        // 3. Attacker replays old token
        await Assert.ThrowsAsync<AuthenticationException>(() =>
            _authService.RefreshTokenAsync(loginResult.RawRefreshToken, "10.0.0.5", "AttackerAgent"));

        // 4. Verify all tokens for this user are now revoked
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == _userId && t.RevokedAt == null)
            .ToListAsync();
        Assert.Empty(activeTokens);

        // 5. Verify security audit log exists
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "SecurityAlert_RefreshTokenReplay");
        Assert.NotNull(auditLog);
        Assert.Equal(_userId, auditLog!.UserId);
    }

    [Fact]
    public async Task RevokeTokenAsync_OnLogout_ShouldMarkTokenRevokedAndWriteAuditLog()
    {
        var loginResult = await _authService.LoginAsync(new LoginRequestDto
        {
            Username = "alicesmith",
            Password = "SecureP@ssword123!"
        }, "127.0.0.1", "UnitTestAgent");

        await _authService.RevokeTokenAsync(loginResult.RawRefreshToken, "127.0.0.1");

        var tokenHash = _jwtTokenService.HashRefreshToken(loginResult.RawRefreshToken);
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        Assert.NotNull(token);
        Assert.NotNull(token!.RevokedAt);

        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "UserLogout");
        Assert.NotNull(auditLog);
        Assert.Equal(_userId, auditLog!.UserId);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
