using System.Net.Http.Json;
using DO.OneAccess.Application.DTOs.Divisions;

namespace DO.OneAccess.Client.Services.Api;

public class DivisionApiClient : IDivisionApiClient
{
    private readonly HttpClient _httpClient;

    public DivisionApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<DivisionDto>> GetAllDivisionsAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("api/divisions", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<DivisionDto>>(cancellationToken: ct);
        return result ?? Array.Empty<DivisionDto>();
    }

    public async Task<DivisionDto> GetDivisionByIdAsync(int divisionId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"api/divisions/{divisionId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<DivisionDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task<DivisionDto> CreateDivisionAsync(CreateDivisionDto dto, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/divisions", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<DivisionDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task<DivisionDto> UpdateDivisionAsync(int divisionId, UpdateDivisionDto dto, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/divisions/{divisionId}", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<DivisionDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task DeactivateDivisionAsync(int divisionId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/divisions/{divisionId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }
    }
}
