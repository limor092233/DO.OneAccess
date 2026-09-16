using System.Net.Http.Json;
using DO.OneAccess.Application.DTOs.Setup;

namespace DO.OneAccess.Client.Services.Api;

public class SetupApiClient : ISetupApiClient
{
    private readonly HttpClient _httpClient;

    public SetupApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SetupStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("api/setup/status", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<SetupStatusDto>(cancellationToken: ct);
        return result ?? new SetupStatusDto { IsInitialized = false };
    }

    public async Task<FirstRunSetupResultDto> SetupAsync(
        FirstRunSetupDto dto,
        string bootstrapToken,
        CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/setup")
        {
            Content = JsonContent.Create(dto)
        };

        if (!string.IsNullOrWhiteSpace(bootstrapToken))
        {
            request.Headers.Add("X-Bootstrap-Token", bootstrapToken.Trim());
        }

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<FirstRunSetupResultDto>(cancellationToken: ct);
        return result ?? new FirstRunSetupResultDto { Success = true, Message = "Setup succeeded." };
    }
}
