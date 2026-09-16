using System.Net.Http.Json;
using DO.OneAccess.Application.DTOs.Sections;

namespace DO.OneAccess.Client.Services.Api;

public class SectionApiClient : ISectionApiClient
{
    private readonly HttpClient _httpClient;

    public SectionApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<SectionDto>> GetSectionsAsync(SectionQueryDto query, CancellationToken ct = default)
    {
        var queryString = $"api/sections?page={query.Page}&pageSize={query.PageSize}";
        if (query.IsActive.HasValue)
        {
            queryString += $"&isActive={query.IsActive.Value.ToString().ToLowerInvariant()}";
        }
        if (query.DivisionId.HasValue)
        {
            queryString += $"&divisionId={query.DivisionId.Value}";
        }

        var response = await _httpClient.GetAsync(queryString, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<SectionDto>>(cancellationToken: ct);
        return result ?? Array.Empty<SectionDto>();
    }

    public async Task<SectionDto> GetSectionByIdAsync(int sectionId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"api/sections/{sectionId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<SectionDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task<SectionDto> CreateSectionAsync(CreateSectionDto dto, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/sections", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<SectionDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task<SectionDto> UpdateSectionAsync(int sectionId, UpdateSectionDto dto, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/sections/{sectionId}", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<SectionDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task DeactivateSectionAsync(int sectionId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/sections/{sectionId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }
    }
}
