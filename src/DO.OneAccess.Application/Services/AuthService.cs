using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs.Auth;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IApplicationDbContext _context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        this._context = _context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthTokenResult> LoginAsync(
        LoginRequestDto request, 
        string? ipAddress, 
        string? userAgent, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AuthenticationException("Invalid username or password.");
        }

        var normalizedUsername = request.Username.Trim();
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == normalizedUsername, cancellationToken);

        if (user == null)
        {
            // Execute dummy verify to prevent timing enumeration
            _passwordHasher.VerifyPassword(new User(), request.Password, "AQAAAAIAAYagAAAAEDummyHashPaddingForConstantTimeComparison==");

            _context.LoginHistories.Add(new LoginHistory
            {
                UserId = null,
                UsernameAttempted = normalizedUsername,
                Success = false,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                FailureReason = "Invalid credentials",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);

            throw new AuthenticationException("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            _context.LoginHistories.Add(new LoginHistory
            {
                UserId = user.UserId,
                UsernameAttempted = normalizedUsername,
                Success = false,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                FailureReason = "User inactive",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);

            throw new AuthenticationException("Invalid username or password.");
        }

        var verifyResult = _passwordHasher.VerifyPassword(user, request.Password, user.PasswordHash);
        if (!verifyResult.Verified)
        {
            _context.LoginHistories.Add(new LoginHistory
            {
                UserId = user.UserId,
                UsernameAttempted = normalizedUsername,
                Success = false,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                FailureReason = "Invalid credentials",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);

            throw new AuthenticationException("Invalid username or password.");
        }

        // Transparent re-hash if algorithm or iteration work factor has been upgraded
        if (verifyResult.RehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            user.UpdatedAt = DateTime.UtcNow;
        }

        user.LastLoginAt = DateTime.UtcNow;

        var accessToken = _jwtTokenService.GenerateAccessToken(user, user.Role.Code);
        var (rawRefreshToken, tokenHash, expiresAt) = _jwtTokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
        _context.RefreshTokens.Add(refreshToken);

        _context.LoginHistories.Add(new LoginHistory
        {
            UserId = user.UserId,
            UsernameAttempted = normalizedUsername,
            Success = true,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthTokenResult
        {
            AccessToken = accessToken,
            ExpiresIn = _jwtTokenService.AccessTokenLifetimeSeconds,
            TokenType = "Bearer",
            RawRefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = expiresAt
        };
    }

    public async Task<AuthTokenResult> RefreshTokenAsync(
        string rawRefreshToken, 
        string? ipAddress, 
        string? userAgent, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            throw new AuthenticationException("Invalid refresh token.");
        }

        var tokenHash = _jwtTokenService.HashRefreshToken(rawRefreshToken);
        var token = await _context.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token == null)
        {
            throw new AuthenticationException("Invalid refresh token.");
        }

        // Replay / reuse detection: Token already marked as revoked or replaced
        if (token.RevokedAt != null || token.ReplacedByTokenId != null)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(t => t.UserId == token.UserId && t.RevokedAt == null)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var active in activeTokens)
            {
                active.RevokedAt = now;
                active.RevokedByIp = ipAddress ?? "ReplayAttackDetected";
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = token.UserId,
                Action = "SecurityAlert_RefreshTokenReplay",
                EntityName = "RefreshTokens",
                EntityId = token.RefreshTokenId.ToString(),
                IpAddress = ipAddress,
                CreatedAt = now,
                OldValues = $"TokenId {token.RefreshTokenId} reused after revocation"
            });

            await _context.SaveChangesAsync(cancellationToken);

            throw new AuthenticationException("Invalid refresh token.");
        }

        if (token.ExpiresAt <= DateTime.UtcNow)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            await _context.SaveChangesAsync(cancellationToken);

            throw new AuthenticationException("Refresh token has expired.");
        }

        if (!token.User.IsActive)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            await _context.SaveChangesAsync(cancellationToken);

            throw new AuthenticationException("User account is inactive.");
        }

        var newAccessToken = _jwtTokenService.GenerateAccessToken(token.User, token.User.Role.Code);
        var (newRawRefreshToken, newTokenHash, newExpiresAt) = _jwtTokenService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            UserId = token.UserId,
            TokenHash = newTokenHash,
            ExpiresAt = newExpiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Mark previous token rotated & linked
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReplacedByTokenId = newRefreshToken.RefreshTokenId;
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthTokenResult
        {
            AccessToken = newAccessToken,
            ExpiresIn = _jwtTokenService.AccessTokenLifetimeSeconds,
            TokenType = "Bearer",
            RawRefreshToken = newRawRefreshToken,
            RefreshTokenExpiresAt = newExpiresAt
        };
    }

    public async Task RevokeTokenAsync(
        string? rawRefreshToken, 
        string? ipAddress, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return;
        }

        var tokenHash = _jwtTokenService.HashRefreshToken(rawRefreshToken);
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token != null && token.RevokedAt == null)
        {
            var now = DateTime.UtcNow;
            token.RevokedAt = now;
            token.RevokedByIp = ipAddress;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = token.UserId,
                Action = "UserLogout",
                EntityName = "RefreshTokens",
                EntityId = token.RefreshTokenId.ToString(),
                IpAddress = ipAddress,
                CreatedAt = now
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
