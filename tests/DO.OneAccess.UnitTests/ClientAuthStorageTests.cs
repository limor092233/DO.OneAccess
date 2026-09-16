using DO.OneAccess.Client.Services;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class ClientAuthStorageTests
{
    [Fact]
    public void ClientAuthService_ShouldMaintainTokenInMemoryWithoutLocalStorage()
    {
        // Assert that client assembly has no references to WebAssembly.LocalStorage or browser storage APIs
        var clientAssembly = typeof(ClientAuthService).Assembly;
        var referencedAssemblies = clientAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(referencedAssemblies, a => a.Name?.Contains("LocalStorage", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(referencedAssemblies, a => a.Name?.Contains("SessionStorage", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task JwtAuthenticationStateProvider_WhenNoTokenPresent_ShouldReturnAnonymous()
    {
        var mockAuthService = new MockClientAuthService(null);
        var provider = new JwtAuthenticationStateProvider(mockAuthService);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.NotNull(state.User);
        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task ClientAuthService_WhenMultipleConcurrentRefreshCalls_ExecutesSingleHttpRequest()
    {
        var httpCalls = 0;
        var mockHandler = new MockHttpMessageHandler(async (req, ct) =>
        {
            if (req.RequestUri?.ToString().Contains("api/auth/refresh") == true)
            {
                Interlocked.Increment(ref httpCalls);
                await Task.Delay(50);
                var content = System.Text.Json.JsonSerializer.Serialize(new Application.DTOs.Auth.RefreshResponseDto
                {
                    AccessToken = "shared_new_token",
                    ExpiresIn = 900
                });
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://localhost/") };
        var jsRuntime = new StubJsRuntime();
        var authService = new ClientAuthService(httpClient, jsRuntime);

        // Dispatch 5 concurrent refresh requests
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => authService.RefreshAsync())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r));
        Assert.Equal("shared_new_token", authService.GetAccessToken());
        // Exactly ONE HTTP request to api/auth/refresh occurred
        Assert.Equal(1, httpCalls);
    }

    private class MockClientAuthService : IClientAuthService
    {
        private readonly string? _token;
#pragma warning disable CS0067
        public event Action? AuthenticationStateChanged;
#pragma warning restore CS0067

        public MockClientAuthService(string? token)
        {
            _token = token;
        }

        public void TriggerStateChanged() => AuthenticationStateChanged?.Invoke();
        public string? GetAccessToken() => _token;
        public Task<bool> LoginAsync(Application.DTOs.Auth.LoginRequestDto request) => Task.FromResult(true);
        public Task<bool> RefreshAsync() => Task.FromResult(true);
        public Task LogoutAsync() => Task.CompletedTask;
    }

    private class StubJsRuntime : Microsoft.JSInterop.IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => ValueTask.FromResult(default(TValue)!);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
