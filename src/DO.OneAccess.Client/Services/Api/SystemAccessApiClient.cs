using System.Net.Http.Json;
using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Access;

namespace DO.OneAccess.Client.Services.Api;

public class SystemAccessApiClient : ISystemAccessApiClient
{
    private readonly HttpClient _httpClient;

    public SystemAccessApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<UserSystemAccessDto>> GetOverridesAsync(
        Guid? userId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var queryString = $"api/system-access?page={page}&pageSize={pageSize}";
        if (userId.HasValue)
        {
            queryString += $"&userId={userId.Value}";
        }

        var response = await _httpClient.GetAsync(queryString, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<PagedResult<UserSystemAccessDto>>(cancellationToken: ct);
        return result ?? new PagedResult<UserSystemAccessDto>();
    }

    public async Task<UserSystemAccessDto> SetAccessAsync(SetUserSystemAccessDto dto, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/system-access", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<UserSystemAccessDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task RevokeAccessAsync(long id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/system-access/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }
    }
}
