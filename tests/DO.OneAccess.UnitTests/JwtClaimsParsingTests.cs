#pragma warning disable CS0067
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DO.OneAccess.Application.DTOs.Auth;
using DO.OneAccess.Client.Services;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class JwtClaimsParsingTests
{
    private class StubClientAuthService : IClientAuthService
    {
        private readonly string? _token;
        public event Action? AuthenticationStateChanged;

        public StubClientAuthService(string? token)
        {
            _token = token;
        }

        public string? GetAccessToken() => _token;
        public Task<bool> LoginAsync(LoginRequestDto request) => Task.FromResult(true);
        public Task<bool> RefreshAsync() => Task.FromResult(true);
        public Task LogoutAsync() => Task.CompletedTask;
    }

    private static string CreateJwtToken(Dictionary<string, object> payload)
    {
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var signature = "fake_signature_for_testing";
        return $"{header}.{payloadBase64}.{signature}";
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WhenTokenIsNull_ReturnsAnonymousState()
    {
        var authService = new StubClientAuthService(null);
        var provider = new JwtAuthenticationStateProvider(authService);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.NotNull(state.User);
        Assert.False(state.User.Identity?.IsAuthenticated);
        Assert.Empty(state.User.Claims);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WhenTokenIsWhitespace_ReturnsAnonymousState()
    {
        var authService = new StubClientAuthService("   ");
        var provider = new JwtAuthenticationStateProvider(authService);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.NotNull(state.User);
        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WhenTokenIsMalformed_ReturnsAnonymousStateSafely()
    {
        var authService = new StubClientAuthService("not.a.valid.jwt.base64!!!");
        var provider = new JwtAuthenticationStateProvider(authService);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.NotNull(state.User);
        Assert.Empty(state.User.Claims);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WhenValidJwt_ParsesClaimsCorrectly()
    {
        var userId = Guid.NewGuid().ToString();
        var payload = new Dictionary<string, object>
        {
            ["sub"] = userId,
            ["name"] = "johndoe",
            ["role"] = "ADMINISTRATOR",
            ["email"] = "johndoe@office.local"
        };
        var jwt = CreateJwtToken(payload);

        var authService = new StubClientAuthService(jwt);
        var provider = new JwtAuthenticationStateProvider(authService);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.NotNull(state.User);
        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal(userId, state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("johndoe", state.User.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal("ADMINISTRATOR", state.User.FindFirst(ClaimTypes.Role)?.Value);
        Assert.Equal("johndoe@office.local", state.User.FindFirst("email")?.Value);
        Assert.True(state.User.IsInRole("ADMINISTRATOR"));
        Assert.False(state.User.IsInRole("SYSTEM_ADMINISTRATOR"));
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WhenRoleIsArray_ParsesAllRoles()
    {
        var payload = new Dictionary<string, object>
        {
            ["sub"] = Guid.NewGuid().ToString(),
            ["name"] = "multirole",
            ["role"] = new[] { "USER", "ADMINISTRATOR" }
        };
        var jwt = CreateJwtToken(payload);

        var authService = new StubClientAuthService(jwt);
        var provider = new JwtAuthenticationStateProvider(authService);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.NotNull(state.User);
        Assert.True(state.User.IsInRole("USER"));
        Assert.True(state.User.IsInRole("ADMINISTRATOR"));
        Assert.False(state.User.IsInRole("SYSTEM_ADMINISTRATOR"));
    }
}
