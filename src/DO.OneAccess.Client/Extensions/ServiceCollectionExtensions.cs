using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using DO.OneAccess.Client.Handlers;
using DO.OneAccess.Client.Services;
using DO.OneAccess.Client.Services.Api;

namespace DO.OneAccess.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientServices(this IServiceCollection services, string apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            apiBaseUrl = "http://localhost:5111";
        }

        if (!apiBaseUrl.EndsWith('/'))
        {
            apiBaseUrl += "/";
        }

        var baseUri = new Uri(apiBaseUrl);

        // Dedicated unauthenticated HTTP client for authentication operations (login, refresh, logout)
        // This eliminates circular dependency: IClientAuthService does NOT depend on an authorized HttpClient
        services.AddScoped<IClientAuthService>(sp =>
        {
            var jsRuntime = sp.GetRequiredService<IJSRuntime>();
            var authHttpClient = new HttpClient
            {
                BaseAddress = baseUri
            };
            return new ClientAuthService(authHttpClient, jsRuntime);
        });

        // Dedicated unauthenticated HTTP client for setup operations
        services.AddScoped<ISetupApiClient>(sp =>
        {
            var setupHttpClient = new HttpClient
            {
                BaseAddress = baseUri
            };
            return new SetupApiClient(setupHttpClient);
        });

        services.AddScoped<ISetupStateService, SetupStateService>();

        // Authentication state provider
        services.AddScoped<JwtAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
        services.AddAuthorizationCore();

        // DelegatingHandler and authorized HttpClient for application API clients
        services.AddTransient<CustomAuthorizationMessageHandler>();
        services.AddScoped(sp =>
        {
            var handler = sp.GetRequiredService<CustomAuthorizationMessageHandler>();
            handler.InnerHandler = new HttpClientHandler();
            return new HttpClient(handler)
            {
                BaseAddress = baseUri
            };
        });

        // Register typed API client services
        services.AddScoped<ISystemApiClient, SystemApiClient>();
        services.AddScoped<IUserApiClient, UserApiClient>();
        services.AddScoped<IDivisionApiClient, DivisionApiClient>();
        services.AddScoped<ISectionApiClient, SectionApiClient>();
        services.AddScoped<ISystemAccessApiClient, SystemAccessApiClient>();
        services.AddScoped<IAdminScopeApiClient, AdminScopeApiClient>();
        services.AddScoped<IAuditApiClient, AuditApiClient>();

        return services;
    }
}
