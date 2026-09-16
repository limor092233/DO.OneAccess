using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using DO.OneAccess.Application.DTOs.Auth;

namespace DO.OneAccess.Client.Services;

public class ClientAuthService : IClientAuthService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private string? _accessToken;
    private Timer? _refreshTimer;

    public event Action? AuthenticationStateChanged;

    public ClientAuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public string? GetAccessToken() => _accessToken;

    public async Task<bool> LoginAsync(LoginRequestDto request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        var response = await _httpClient.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
        {
            ClearSession();
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        if (result == null || string.IsNullOrWhiteSpace(result.AccessToken))
        {
            ClearSession();
            return false;
        }

        SetSession(result.AccessToken, result.ExpiresIn);
        return true;
    }

    private readonly object _refreshSync = new();
    private Task<bool>? _inFlightRefreshTask;

    public Task<bool> RefreshAsync()
    {
        lock (_refreshSync)
        {
            if (_inFlightRefreshTask != null && !_inFlightRefreshTask.IsCompleted)
            {
                return _inFlightRefreshTask;
            }

            _inFlightRefreshTask = ExecuteRefreshAsync();
            return _inFlightRefreshTask;
        }
    }

    private async Task<bool> ExecuteRefreshAsync()
    {
        try
        {
            var xsrfToken = await GetXsrfTokenAsync();

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh");
            httpRequest.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            if (!string.IsNullOrWhiteSpace(xsrfToken))
            {
                httpRequest.Headers.Add("X-XSRF-TOKEN", xsrfToken);
            }

            var response = await _httpClient.SendAsync(httpRequest);
            if (!response.IsSuccessStatusCode)
            {
                ClearSession();
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<RefreshResponseDto>();
            if (result == null || string.IsNullOrWhiteSpace(result.AccessToken))
            {
                ClearSession();
                return false;
            }

            SetSession(result.AccessToken, result.ExpiresIn);
            return true;
        }
        catch
        {
            ClearSession();
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        var xsrfToken = await GetXsrfTokenAsync();

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
        httpRequest.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        if (!string.IsNullOrWhiteSpace(xsrfToken))
        {
            httpRequest.Headers.Add("X-XSRF-TOKEN", xsrfToken);
        }

        if (!string.IsNullOrWhiteSpace(_accessToken))
        {
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        }

        try
        {
            await _httpClient.SendAsync(httpRequest);
        }
        catch
        {
            // Best-effort logout attempt over network
        }
        finally
        {
            ClearSession();
        }
    }

    private void SetSession(string token, int expiresInSeconds)
    {
        _accessToken = token;
        ScheduleRefresh(expiresInSeconds);
        AuthenticationStateChanged?.Invoke();
    }

    private void ClearSession()
    {
        _accessToken = null;
        _refreshTimer?.Dispose();
        _refreshTimer = null;
        AuthenticationStateChanged?.Invoke();
    }

    private void ScheduleRefresh(int expiresInSeconds)
    {
        _refreshTimer?.Dispose();

        // Refresh 60 seconds before expiration, or halfway if lifetime is under 2 minutes
        var refreshLeadTime = expiresInSeconds > 120 ? 60 : expiresInSeconds / 2;
        var dueSeconds = Math.Max(10, expiresInSeconds - refreshLeadTime);

        _refreshTimer = new Timer(async _ =>
        {
            await RefreshAsync();
        }, null, TimeSpan.FromSeconds(dueSeconds), Timeout.InfiniteTimeSpan);
    }

    private async Task<string?> GetXsrfTokenAsync()
    {
        try
        {
            var cookieString = await _jsRuntime.InvokeAsync<string>("eval", "document.cookie");
            if (string.IsNullOrEmpty(cookieString))
            {
                return null;
            }

            foreach (var part in cookieString.Split(';'))
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("XSRF-TOKEN=", StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(trimmed["XSRF-TOKEN=".Length..]);
                }
            }
        }
        catch
        {
            // Fallback if JS interop not ready
        }

        return null;
    }

    public void Dispose()
    {
        _refreshTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
