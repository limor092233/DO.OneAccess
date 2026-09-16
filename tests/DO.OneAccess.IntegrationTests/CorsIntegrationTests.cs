using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DO.OneAccess.Infrastructure.Persistence;
using Xunit;

namespace DO.OneAccess.IntegrationTests;

[Collection("IntegrationTests")]
public class CorsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string TestDatabaseName = "DO_OneAccess_Test";
    private const string ConnectionString = $"Server=localhost\\SQLEXPRESS;Database={TestDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CorsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Enforce strict safety guard: NEVER target DO_OneAccess, DO_OneAccess_DB, or DO_NSHP_APP_DB
        var builder = new SqlConnectionStringBuilder(ConnectionString);
        if (!string.Equals(builder.InitialCatalog, TestDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Security Guard: Integration tests must strictly target '{TestDatabaseName}', but targeted '{builder.InitialCatalog}'.");
        }

        if (string.Equals(builder.InitialCatalog, "DO_OneAccess", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(builder.InitialCatalog, "DO_OneAccess_DB", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(builder.InitialCatalog, "DO_NSHP_APP_DB", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Security Guard Violation: Attempted to target protected database '{builder.InitialCatalog}'.");
        }

        _factory = factory.WithWebHostBuilder(hostBuilder =>
        {
            hostBuilder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Issuer", "DO.OneAccess" },
                    { "Jwt:Audience", "DO.OneAccess.API" },
                    { "Jwt:Key", "IntegrationTestingSecureSigningKey32BytesLong!" },
                    { "Security:UseHostCookiePrefix", "false" },
                    { "Security:RequireHttpsCookie", "false" }
                });
            });

            hostBuilder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
                if (contextDescriptor != null) services.Remove(contextDescriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(ConnectionString));
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Preflight_AllowedOrigin_Http5202_ReturnsCorsHeaders()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "http://localhost:5202");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,x-xsrf-token");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Preflight request failed with status: {response.StatusCode}");
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("http://localhost:5202", response.Headers.GetValues("Access-Control-Allow-Origin").First());
        Assert.True(response.Headers.Contains("Access-Control-Allow-Credentials"));
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").First());
    }

    [Fact]
    public async Task Preflight_AllowedOrigin_Https7030_ReturnsCorsHeaders()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/refresh");
        request.Headers.Add("Origin", "https://localhost:7030");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,x-xsrf-token");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Preflight request failed with status: {response.StatusCode}");
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("https://localhost:7030", response.Headers.GetValues("Access-Control-Allow-Origin").First());
        Assert.True(response.Headers.Contains("Access-Control-Allow-Credentials"));
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").First());
    }

    [Fact]
    public async Task Preflight_DisallowedOrigin_DoesNotReturnAllowOriginHeader()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "http://unauthorized-origin.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"),
            "CORS policy must NOT return Access-Control-Allow-Origin for untrusted/disallowed origins.");
    }

    [Fact]
    public async Task ActualRequest_AllowedOrigin_ReturnsAllowOriginAndCredentials()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        request.Headers.Add("Origin", "http://localhost:5202");
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("http://localhost:5202", response.Headers.GetValues("Access-Control-Allow-Origin").First());
        Assert.True(response.Headers.Contains("Access-Control-Allow-Credentials"));
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").First());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
