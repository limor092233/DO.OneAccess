using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;
using DO.OneAccess.Client.Services;

namespace DO.OneAccess.Client.Handlers;

public class CustomAuthorizationMessageHandler : DelegatingHandler
{
    private readonly IClientAuthService _authService;
    private readonly NavigationManager _navigationManager;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);
    private static readonly HttpRequestOptionsKey<bool> _retriedKey = new("Is401Retried");

    private static string? _lastFailedToken;

    public CustomAuthorizationMessageHandler(IClientAuthService authService, NavigationManager navigationManager)
    {
        _authService = authService;
        _navigationManager = navigationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;

        // Rule: Authentication endpoints must not trigger token attachment or refresh recursion
        if (path.Contains("/api/auth/", StringComparison.OrdinalIgnoreCase))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Attach current in-memory Bearer token
        var initialToken = _authService.GetAccessToken();
        if (!string.IsNullOrWhiteSpace(initialToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", initialToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // If not 401 Unauthorized, return response immediately
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        // Rule: An original request may be retried at most once
        if (request.Options.TryGetValue(_retriedKey, out var alreadyRetried) && alreadyRetried)
        {
            return response;
        }

        // Mark this request as having attempted a 401 retry to prevent infinite loops
        request.Options.Set(_retriedKey, true);

        // Concurrency Control: Only one refresh operation may execute at a time
        // Concurrent requests encountering 401 share/await the same refresh operation
        bool refreshSucceeded;
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var currentToken = _authService.GetAccessToken();
            if (!string.IsNullOrWhiteSpace(currentToken) && currentToken != initialToken)
            {
                refreshSucceeded = true;
            }
            else if (string.IsNullOrWhiteSpace(currentToken) && initialToken != null && _lastFailedToken == initialToken)
            {
                refreshSucceeded = false;
            }
            else
            {
                refreshSucceeded = await _authService.RefreshAsync();
                _lastFailedToken = refreshSucceeded ? null : initialToken;
            }
        }
        finally
        {
            _refreshLock.Release();
        }

        if (!refreshSucceeded)
        {
            // Refresh failure: clear authentication state and redirect to /login
            if (!string.IsNullOrWhiteSpace(_authService.GetAccessToken()))
            {
                await _authService.LogoutAsync();
            }

            var currentUri = _navigationManager.ToBaseRelativePath(_navigationManager.Uri);
            if (!currentUri.StartsWith("login", StringComparison.OrdinalIgnoreCase))
            {
                _navigationManager.NavigateTo("login");
            }

            return response;
        }

        // Retry the original request exactly once with the new access token
        var newToken = _authService.GetAccessToken();
        if (!string.IsNullOrWhiteSpace(newToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
