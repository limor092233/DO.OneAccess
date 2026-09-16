using System.Net.Http.Json;
using DO.OneAccess.Application.DTOs.Access;

namespace DO.OneAccess.Client.Services.Api;

public class AdminScopeApiClient : IAdminScopeApiClient
{
    private readonly HttpClient _httpClient;

    public AdminScopeApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<AdminScopeDto>> GetAllScopesAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("api/admin-scopes", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<AdminScopeDto>>(cancellationToken: ct);
        return result ?? Array.Empty<AdminScopeDto>();
    }

    public async Task<AdminScopeDto> AssignDivisionScopeAsync(AssignAdminScopeDto dto, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/admin-scopes", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<AdminScopeDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task RevokeDivisionScopeAsync(long id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/admin-scopes/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }
    }

    public async Task<IReadOnlyList<AdminSystemAccessDto>> GetAllGrantsAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("api/admin-system-access", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<AdminSystemAccessDto>>(cancellationToken: ct);
        return result ?? Array.Empty<AdminSystemAccessDto>();
    }

    public async Task<AdminSystemAccessDto> GrantAccessAsync(GrantAdminSystemAccessDto dto, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/admin-system-access", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<AdminSystemAccessDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task RevokeAccessAsync(long id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/admin-system-access/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }
    }
}
