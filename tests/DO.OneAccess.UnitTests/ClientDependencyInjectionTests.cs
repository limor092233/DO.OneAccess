using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using DO.OneAccess.Client.Extensions;
using DO.OneAccess.Client.Handlers;
using DO.OneAccess.Client.Services;
using DO.OneAccess.Client.Services.Api;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class ClientDependencyInjectionTests
{
    private class DummyJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            ValueTask.FromResult(default(TValue)!);
    }

    private class DummyNavigationManager : NavigationManager
    {
        public DummyNavigationManager()
        {
            Initialize("http://localhost:5202/", "http://localhost:5202/");
        }

        protected override void NavigateToCore(string uri, NavigationOptions options) { }
    }

    private static IServiceProvider CreateServiceProvider(string apiBaseUrl = "http://localhost:5111")
    {
        var services = new ServiceCollection();
        services.AddScoped<IJSRuntime, DummyJSRuntime>();
        services.AddScoped<NavigationManager, DummyNavigationManager>();
        services.AddClientServices(apiBaseUrl);
        return services.BuildServiceProvider();
    }

    private static HttpMessageHandler? GetHttpMessageHandler(HttpClient client)
    {
        var handlerField = typeof(HttpMessageInvoker).GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic);
        return handlerField?.GetValue(client) as HttpMessageHandler;
    }

    private static HttpClient? GetAuthServiceClient(IClientAuthService authService)
    {
        var clientField = typeof(ClientAuthService).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);
        return clientField?.GetValue(authService) as HttpClient;
    }

    [Fact]
    public void Resolve_IClientAuthService_Succeeds_WithoutCircularDependency()
    {
        // Act
        var sp = CreateServiceProvider();
        var authService = sp.GetRequiredService<IClientAuthService>();

        // Assert
        Assert.NotNull(authService);
        Assert.IsType<ClientAuthService>(authService);
    }

    [Fact]
    public void AuthClient_DoesNotUse_CustomAuthorizationMessageHandler()
    {
        // Act
        var sp = CreateServiceProvider();
        var authService = sp.GetRequiredService<IClientAuthService>();
        var authHttpClient = GetAuthServiceClient(authService);

        // Assert
        Assert.NotNull(authHttpClient);
        var handler = GetHttpMessageHandler(authHttpClient);
        Assert.NotNull(handler);
        Assert.IsNotType<CustomAuthorizationMessageHandler>(handler);
        Assert.IsType<HttpClientHandler>(handler);
    }

    [Fact]
    public void Authorized_HttpClient_Uses_CustomAuthorizationMessageHandler()
    {
        // Act
        var sp = CreateServiceProvider();
        var authorizedClient = sp.GetRequiredService<HttpClient>();

        // Assert
        Assert.NotNull(authorizedClient);
        var handler = GetHttpMessageHandler(authorizedClient);
        Assert.NotNull(handler);
        Assert.IsType<CustomAuthorizationMessageHandler>(handler);

        var customHandler = (CustomAuthorizationMessageHandler)handler;
        var innerHandlerProp = typeof(DelegatingHandler).GetProperty("InnerHandler", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var innerHandler = innerHandlerProp?.GetValue(customHandler);
        Assert.NotNull(innerHandler);
        Assert.IsType<HttpClientHandler>(innerHandler);
    }

    [Fact]
    public void Client_ApiBaseUrl_ResolvesToConfiguredServer()
    {
        // Arrange
        const string expectedBaseUrl = "http://localhost:5111/";

        // Act
        var sp = CreateServiceProvider("http://localhost:5111");
        var authorizedClient = sp.GetRequiredService<HttpClient>();
        var authService = sp.GetRequiredService<IClientAuthService>();
        var authHttpClient = GetAuthServiceClient(authService);

        // Assert
        Assert.Equal(new Uri(expectedBaseUrl), authorizedClient.BaseAddress);
        Assert.NotNull(authHttpClient);
        Assert.Equal(new Uri(expectedBaseUrl), authHttpClient.BaseAddress);
    }

    [Fact]
    public void TypedApiClients_Receive_AuthorizedHttpClient()
    {
        // Act
        var sp = CreateServiceProvider();
        var systemApi = sp.GetRequiredService<ISystemApiClient>();

        // Assert
        Assert.NotNull(systemApi);
        Assert.IsType<SystemApiClient>(systemApi);

        var clientField = typeof(SystemApiClient).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);
        var client = clientField?.GetValue(systemApi) as HttpClient;
        Assert.NotNull(client);

        var handler = GetHttpMessageHandler(client);
        Assert.NotNull(handler);
        Assert.IsType<CustomAuthorizationMessageHandler>(handler);
    }
}
