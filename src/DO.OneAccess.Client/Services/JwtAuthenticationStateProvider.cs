using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace DO.OneAccess.Client.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly IClientAuthService _authService;
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public JwtAuthenticationStateProvider(IClientAuthService authService)
    {
        _authService = authService;
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = _authService.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(new AuthenticationState(Anonymous));
        }

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return Task.FromResult(new AuthenticationState(user));
    }

    private void OnAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return claims;
        }

        try
        {
            var payload = parts[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs == null)
            {
                return claims;
            }

            foreach (var kvp in keyValuePairs)
            {
                var key = kvp.Key switch
                {
                    "sub" => ClaimTypes.NameIdentifier,
                    "name" or "unique_name" => ClaimTypes.Name,
                    "role" => ClaimTypes.Role,
                    _ => kvp.Key
                };

                if (kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        claims.Add(new Claim(key, item.ToString()));
                    }
                }
                else
                {
                    claims.Add(new Claim(key, kvp.Value?.ToString() ?? string.Empty));
                }
            }
        }
        catch
        {
            // Malformed token falls back safely to empty claims (anonymous)
        }

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64.Replace('-', '+').Replace('_', '/'));
    }

    public void Dispose()
    {
        _authService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        GC.SuppressFinalize(this);
    }
}
