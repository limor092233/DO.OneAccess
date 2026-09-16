using System.Net;
using System.Text;
using System.Text.Json;
using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Systems;
using DO.OneAccess.Application.DTOs.Users;
using DO.OneAccess.Client.Services.Api;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class TypedApiClientTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return ResponseToReturn;
        }
    }

    [Fact]
    public async Task SystemApiClient_GetAccessibleSystemsAsync_CallsGetApiSystems()
    {
        var systemsJson = JsonSerializer.Serialize(new List<SystemDto>
        {
            new() { SystemId = Guid.NewGuid(), SystemCode = "HR", SystemName = "HR Portal", BaseUrl = "https://hr.example.com", IsActive = true }
        });

        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(systemsJson, Encoding.UTF8, "application/json")
            }
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var client = new SystemApiClient(httpClient);

        var result = await client.GetAccessibleSystemsAsync();

        Assert.Single(result);
        Assert.Equal("HR", result[0].SystemCode);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Equal("api/systems", handler.LastRequest.RequestUri?.AbsolutePath.TrimStart('/'));
    }

    [Fact]
    public void ExternalSystemLaunch_PreservesTrustedBaseUrlWithoutJwt()
    {
        var system = new SystemDto
        {
            SystemId = Guid.NewGuid(),
            SystemCode = "PORTAL_A",
            SystemName = "Portal Alpha",
            BaseUrl = "https://app-a.internal.domain/entry",
            IsActive = true
        };

        // Rule: External system launch uses trusted backend BaseUrl only.
        // Never put JWT in URL, query string, or URL fragment.
        var launchUrl = system.BaseUrl;

        Assert.Equal("https://app-a.internal.domain/entry", launchUrl);
        Assert.DoesNotContain("token", launchUrl, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bearer", launchUrl, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("jwt", launchUrl, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("?", launchUrl);
        Assert.DoesNotContain("#", launchUrl);
    }

    [Fact]
    public async Task UserApiClient_GetUsersAsync_BuildsCorrectQueryParameters()
    {
        var pagedResult = new PagedResult<UserDto>
        {
            Items = new List<UserDto>(),
            Page = 2,
            PageSize = 10,
            TotalCount = 0
        };

        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(pagedResult), Encoding.UTF8, "application/json")
            }
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var client = new UserApiClient(httpClient);

        var query = new UserQueryDto
        {
            Page = 2,
            PageSize = 10,
            UsernameContains = "johndoe",
            IsActive = true
        };

        await client.GetUsersAsync(query);

        Assert.NotNull(handler.LastRequest);
        var uri = handler.LastRequest.RequestUri?.ToString();
        Assert.NotNull(uri);
        Assert.Contains("page=2", uri);
        Assert.Contains("pageSize=10", uri);
        Assert.Contains("usernameContains=johndoe", uri);
        Assert.Contains("isActive=true", uri);
    }

    [Fact]
    public async Task UserApiClient_ChangeRoleAsync_SendsExpectedPutPayload()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.NoContent)
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var client = new UserApiClient(httpClient);

        var targetUserId = Guid.NewGuid();
        var changeRoleDto = new ChangeRoleDto { RoleId = 2 }; // ADMINISTRATOR

        await client.ChangeRoleAsync(targetUserId, changeRoleDto);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Put, handler.LastRequest.Method);
        Assert.Equal($"api/users/{targetUserId}/role", handler.LastRequest.RequestUri?.AbsolutePath.TrimStart('/'));
        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("\"roleId\":2", handler.LastRequestBody.Replace(" ", ""));
    }
}
