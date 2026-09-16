using System.Net.Http.Json;
using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Audit;

namespace DO.OneAccess.Client.Services.Api;

public class AuditApiClient : IAuditApiClient
{
    private readonly HttpClient _httpClient;

    public AuditApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryDto query, CancellationToken ct = default)
    {
        var queryString = $"api/audit-logs?page={query.Page}&pageSize={query.PageSize}";
        if (query.UserId.HasValue)
        {
            queryString += $"&userId={query.UserId.Value}";
        }
        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            queryString += $"&action={Uri.EscapeDataString(query.Action)}";
        }
        if (query.From.HasValue)
        {
            queryString += $"&from={query.From.Value:O}";
        }
        if (query.To.HasValue)
        {
            queryString += $"&to={query.To.Value:O}";
        }

        var response = await _httpClient.GetAsync(queryString, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogDto>>(cancellationToken: ct);
        return result ?? new PagedResult<AuditLogDto>();
    }

    public async Task<PagedResult<LoginHistoryDto>> GetLoginHistoriesAsync(LoginHistoryQueryDto query, CancellationToken ct = default)
    {
        var queryString = $"api/login-histories?page={query.Page}&pageSize={query.PageSize}";
        if (query.UserId.HasValue)
        {
            queryString += $"&userId={query.UserId.Value}";
        }
        if (query.Success.HasValue)
        {
            queryString += $"&success={query.Success.Value.ToString().ToLowerInvariant()}";
        }
        if (query.From.HasValue)
        {
            queryString += $"&from={query.From.Value:O}";
        }
        if (query.To.HasValue)
        {
            queryString += $"&to={query.To.Value:O}";
        }

        var response = await _httpClient.GetAsync(queryString, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<PagedResult<LoginHistoryDto>>(cancellationToken: ct);
        return result ?? new PagedResult<LoginHistoryDto>();
    }
}
