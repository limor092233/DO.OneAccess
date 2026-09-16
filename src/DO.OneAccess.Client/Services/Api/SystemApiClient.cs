using System.Net.Http.Json;
using DO.OneAccess.Application.DTOs.Systems;

namespace DO.OneAccess.Client.Services.Api;

public class SystemApiClient : ISystemApiClient
{
    private readonly HttpClient _httpClient;

    public SystemApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<SystemDto>> GetAccessibleSystemsAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("api/systems", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<SystemDto>>(cancellationToken: ct);
        return result ?? Array.Empty<SystemDto>();
    }

    public async Task<IReadOnlyList<SystemDto>> GetAllSystemsAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("api/systems/all", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<SystemDto>>(cancellationToken: ct);
        return result ?? Array.Empty<SystemDto>();
    }

    public async Task<SystemDto> GetSystemByIdAsync(Guid systemId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"api/systems/{systemId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<SystemDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task<SystemDto> RegisterSystemAsync(RegisterSystemDto dto, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/systems", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<SystemDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task<SystemDto> UpdateSystemAsync(Guid systemId, UpdateSystemDto dto, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/systems/{systemId}", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<SystemDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task DeactivateSystemAsync(Guid systemId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/systems/{systemId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }
    }
}
