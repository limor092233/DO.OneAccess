using System.Net.Http.Json;
using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Users;

namespace DO.OneAccess.Client.Services.Api;

public class UserApiClient : IUserApiClient
{
    private readonly HttpClient _httpClient;

    public UserApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(UserQueryDto query, CancellationToken ct = default)
    {
        var queryString = $"api/users?page={query.Page}&pageSize={query.PageSize}";
        if (query.IsActive.HasValue)
        {
            queryString += $"&isActive={query.IsActive.Value.ToString().ToLowerInvariant()}";
        }
        if (!string.IsNullOrWhiteSpace(query.UsernameContains))
        {
            queryString += $"&usernameContains={Uri.EscapeDataString(query.UsernameContains)}";
        }

        var response = await _httpClient.GetAsync(queryString, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>(cancellationToken: ct);
        return result ?? new PagedResult<UserDto>();
    }

    public async Task<UserDto> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"api/users/{userId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/users", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task TransferSystemAdministratorAsync(TransferSystemAdministratorDto dto, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/users/system-administrator/transfer", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }
    }

    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto dto, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/users/{userId}", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }

        var result = await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Response content was empty.");
    }

    public async Task DeactivateUserAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/users/{userId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }
    }

    public async Task ChangeRoleAsync(Guid userId, ChangeRoleDto dto, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/users/{userId}/role", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await ProblemDetailsReader.ReadFromResponseAsync(response);
        }
    }
}
