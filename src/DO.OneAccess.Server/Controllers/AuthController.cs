using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs.Auth;

namespace DO.OneAccess.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly bool _useHostPrefix;
    private readonly bool _requireHttpsCookie;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
        _useHostPrefix = configuration.GetValue<bool>("Security:UseHostCookiePrefix");
        _requireHttpsCookie = configuration.GetValue<bool>("Security:RequireHttpsCookie");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Username and password are required.");
        }

        try
        {
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            var result = await _authService.LoginAsync(request, ipAddress, userAgent, cancellationToken);

            AppendAuthCookies(result.RawRefreshToken, result.RefreshTokenExpiresAt);

            return Ok(new LoginResponseDto
            {
                AccessToken = result.AccessToken,
                ExpiresIn = result.ExpiresIn,
                TokenType = result.TokenType
            });
        }
        catch (AuthenticationException)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Invalid username or password.");
        }
        catch (Exception)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred.");
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!ValidateCsrf())
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "CSRF token validation failed.");
        }

        var cookieName = GetRefreshCookieName();
        if (!Request.Cookies.TryGetValue(cookieName, out var rawRefreshToken) || string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            ClearAuthCookies();
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Refresh token cookie is missing.");
        }

        try
        {
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            var result = await _authService.RefreshTokenAsync(rawRefreshToken, ipAddress, userAgent, cancellationToken);

            AppendAuthCookies(result.RawRefreshToken, result.RefreshTokenExpiresAt);

            return Ok(new RefreshResponseDto
            {
                AccessToken = result.AccessToken,
                ExpiresIn = result.ExpiresIn,
                TokenType = result.TokenType
            });
        }
        catch (AuthenticationException)
        {
            ClearAuthCookies();
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Invalid or expired refresh token.");
        }
        catch (Exception)
        {
            ClearAuthCookies();
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred.");
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!ValidateCsrf())
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "CSRF token validation failed.");
        }

        try
        {
            var cookieName = GetRefreshCookieName();
            Request.Cookies.TryGetValue(cookieName, out var rawRefreshToken);

            var ipAddress = GetClientIpAddress();
            await _authService.RevokeTokenAsync(rawRefreshToken, ipAddress, cancellationToken);

            ClearAuthCookies();

            return NoContent();
        }
        catch (Exception)
        {
            ClearAuthCookies();
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred.");
        }
    }

    private string GetRefreshCookieName()
    {
        return _useHostPrefix ? "__Host-refreshToken" : "refreshToken";
    }

    private void AppendAuthCookies(string rawRefreshToken, DateTime expiresAt)
    {
        var isSecure = _useHostPrefix || _requireHttpsCookie || Request.IsHttps;

        var refreshOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Strict,
            // __Host- prefix requires Path=/ (enforced by browser spec)
            // plain refreshToken is scoped to auth endpoints only
            Path = _useHostPrefix ? "/" : "/api/auth",
            Expires = expiresAt
        };
        Response.Cookies.Append(GetRefreshCookieName(), rawRefreshToken, refreshOptions);

        var csrfToken = Guid.NewGuid().ToString("N");
        var csrfOptions = new CookieOptions
        {
            HttpOnly = false, // Must be readable by client JS to set X-XSRF-TOKEN header
            Secure = isSecure,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expiresAt
        };
        Response.Cookies.Append("XSRF-TOKEN", csrfToken, csrfOptions);
    }

    private void ClearAuthCookies()
    {
        var isSecure = _useHostPrefix || _requireHttpsCookie || Request.IsHttps;

        var clearRefreshOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Strict,
            // Must match the path used when setting the cookie for deletion to succeed
            Path = _useHostPrefix ? "/" : "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Response.Cookies.Delete(GetRefreshCookieName(), clearRefreshOptions);

        var clearCsrfOptions = new CookieOptions
        {
            HttpOnly = false,
            Secure = isSecure,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Response.Cookies.Delete("XSRF-TOKEN", clearCsrfOptions);
    }

    private bool ValidateCsrf()
    {
        if (!Request.Headers.TryGetValue("X-XSRF-TOKEN", out var headerCsrf) || string.IsNullOrWhiteSpace(headerCsrf))
        {
            return false;
        }

        if (!Request.Cookies.TryGetValue("XSRF-TOKEN", out var cookieCsrf) || string.IsNullOrWhiteSpace(cookieCsrf))
        {
            return false;
        }

        return string.Equals(headerCsrf.ToString(), cookieCsrf, StringComparison.Ordinal);
    }

    private string? GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.ToString().Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
