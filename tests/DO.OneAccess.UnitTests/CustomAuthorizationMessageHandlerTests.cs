#pragma warning disable CS0067
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;
using DO.OneAccess.Application.DTOs.Auth;
using DO.OneAccess.Client.Handlers;
using DO.OneAccess.Client.Services;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class CustomAuthorizationMessageHandlerTests
{
    private class TestNavigationManager : NavigationManager
    {
        public string? NavigatedUri { get; private set; }

        public TestNavigationManager()
        {
            Initialize("https://localhost/", "https://localhost/");
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NavigatedUri = uri;
        }
    }

    private class MockAuthService : IClientAuthService
    {
        private string? _token;
        public int RefreshCallCount { get; private set; }
        public int LogoutCallCount { get; private set; }
        public bool RefreshSuccessResult { get; set; } = true;
        public string? TokenAfterRefresh { get; set; } = "refreshed_token_456";

        public event Action? AuthenticationStateChanged;

        public MockAuthService(string? initialToken)
        {
            _token = initialToken;
        }

        public string? GetAccessToken() => _token;

        public Task<bool> LoginAsync(LoginRequestDto request) => Task.FromResult(true);

        public async Task<bool> RefreshAsync()
        {
            RefreshCallCount++;
            // Simulate brief latency for concurrency testing
            await Task.Delay(50);
            if (RefreshSuccessResult)
            {
                _token = TokenAfterRefresh;
                return true;
            }
            return false;
        }

        public Task LogoutAsync()
        {
            LogoutCallCount++;
            _token = null;
            return Task.CompletedTask;
        }
    }

    private class MockHttpHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> HandlerFunc { get; set; }

        public MockHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
        {
            HandlerFunc = handlerFunc;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return HandlerFunc(request, cancellationToken);
        }
    }

    [Fact]
    public async Task SendAsync_WhenTokenExists_AttachesBearerToken()
    {
        var authService = new MockAuthService("initial_token_123");
        var navManager = new TestNavigationManager();
        AuthenticationHeaderValue? capturedAuth = null;

        var innerHandler = new MockHttpHandler((req, ct) =>
        {
            capturedAuth = req.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var messageHandler = new CustomAuthorizationMessageHandler(authService, navManager)
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(messageHandler) { BaseAddress = new Uri("https://localhost/") };
        var response = await client.GetAsync("api/systems");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capturedAuth);
        Assert.Equal("Bearer", capturedAuth.Scheme);
        Assert.Equal("initial_token_123", capturedAuth.Parameter);
    }

    [Fact]
    public async Task SendAsync_ForAuthEndpoints_ExcludesBearerTokenAndDoesNotTriggerRefresh()
    {
        var authService = new MockAuthService("initial_token_123");
        var navManager = new TestNavigationManager();
        AuthenticationHeaderValue? capturedAuth = null;

        var innerHandler = new MockHttpHandler((req, ct) =>
        {
            capturedAuth = req.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        });

        var messageHandler = new CustomAuthorizationMessageHandler(authService, navManager)
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(messageHandler) { BaseAddress = new Uri("https://localhost/") };
        var response = await client.PostAsync("api/auth/login", new StringContent("{}"));

        // Should return 401 without attaching bearer or triggering refresh
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(capturedAuth);
        Assert.Equal(0, authService.RefreshCallCount);
    }

    [Fact]
    public async Task SendAsync_When401Encountered_RefreshesAndRetriesOnce()
    {
        var authService = new MockAuthService("expired_token");
        var navManager = new TestNavigationManager();
        var callCount = 0;
        var capturedTokens = new List<string?>();

        var innerHandler = new MockHttpHandler((req, ct) =>
        {
            callCount++;
            capturedTokens.Add(req.Headers.Authorization?.Parameter);

            if (callCount == 1)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var messageHandler = new CustomAuthorizationMessageHandler(authService, navManager)
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(messageHandler) { BaseAddress = new Uri("https://localhost/") };
        var response = await client.GetAsync("api/systems");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, callCount); // Original + 1 retry
        Assert.Equal(1, authService.RefreshCallCount);
        Assert.Equal("expired_token", capturedTokens[0]);
        Assert.Equal("refreshed_token_456", capturedTokens[1]);
    }

    [Fact]
    public async Task SendAsync_WhenRetriedRequestAlsoReturns401_DoesNotLoopInfinitely()
    {
        var authService = new MockAuthService("expired_token");
        var navManager = new TestNavigationManager();
        var callCount = 0;

        var innerHandler = new MockHttpHandler((req, ct) =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        });

        var messageHandler = new CustomAuthorizationMessageHandler(authService, navManager)
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(messageHandler) { BaseAddress = new Uri("https://localhost/") };
        var response = await client.GetAsync("api/systems");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(2, callCount); // Max 1 retry
        Assert.Equal(1, authService.RefreshCallCount);
    }

    [Fact]
    public async Task SendAsync_WhenRefreshFails_ClearsStateAndNavigatesToLogin()
    {
        var authService = new MockAuthService("expired_token")
        {
            RefreshSuccessResult = false
        };
        var navManager = new TestNavigationManager();

        var innerHandler = new MockHttpHandler((req, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        });

        var messageHandler = new CustomAuthorizationMessageHandler(authService, navManager)
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(messageHandler) { BaseAddress = new Uri("https://localhost/") };
        var response = await client.GetAsync("api/systems");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, authService.RefreshCallCount);
        Assert.Equal(1, authService.LogoutCallCount);
        Assert.Equal("login", navManager.NavigatedUri);
    }

    [Fact]
    public async Task SendAsync_WhenConcurrent401Requests_ExecutesExactlyOneRefresh()
    {
        var authService = new MockAuthService("expired_concurrent_token");
        var navManager = new TestNavigationManager();
        var requestCount = 0;

        var innerHandler = new MockHttpHandler(async (req, ct) =>
        {
            Interlocked.Increment(ref requestCount);
            var token = req.Headers.Authorization?.Parameter;

            if (token == "expired_concurrent_token")
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var messageHandler = new CustomAuthorizationMessageHandler(authService, navManager)
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(messageHandler) { BaseAddress = new Uri("https://localhost/") };

        // Fire 5 concurrent requests that will all receive 401 initially
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => client.GetAsync("api/systems"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // All 5 should ultimately succeed with OK after the single shared refresh
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        // Exactly ONE refresh should have been executed by the handler
        Assert.Equal(1, authService.RefreshCallCount);
    }

    [Fact]
    public async Task SendAsync_WhenConcurrent401RequestsAndRefreshFails_ExecutesExactlyOneRefreshAndSingleLogout()
    {
        var authService = new MockAuthService("expired_token_failure")
        {
            RefreshSuccessResult = false
        };
        var navManager = new TestNavigationManager();

        var innerHandler = new MockHttpHandler((req, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        });

        var messageHandler = new CustomAuthorizationMessageHandler(authService, navManager)
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(messageHandler) { BaseAddress = new Uri("https://localhost/") };

        // 5 concurrent requests failing with 401
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => client.GetAsync("api/systems"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // All 5 should return Unauthorized
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));
        // Exactly ONE refresh attempted across all concurrent failures
        Assert.Equal(1, authService.RefreshCallCount);
        // Exactly ONE logout executed
        Assert.Equal(1, authService.LogoutCallCount);
        Assert.Equal("login", navManager.NavigatedUri);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SendAsync_WhenNon401StatusReturned_DoesNotTriggerRefreshAndReturnsResponseDirectly(HttpStatusCode statusCode)
    {
        var authService = new MockAuthService("valid_token");
        var navManager = new TestNavigationManager();
        var callCount = 0;

        var innerHandler = new MockHttpHandler((req, ct) =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        });

        var messageHandler = new CustomAuthorizationMessageHandler(authService, navManager)
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(messageHandler) { BaseAddress = new Uri("https://localhost/") };
        var response = await client.GetAsync("api/employees");

        // The response must be returned directly without triggering refresh or logout
        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal(1, callCount);
        Assert.Equal(0, authService.RefreshCallCount);
        Assert.Equal(0, authService.LogoutCallCount);
        Assert.Null(navManager.NavigatedUri);
    }
}

