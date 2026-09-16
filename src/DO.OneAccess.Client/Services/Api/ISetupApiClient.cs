using DO.OneAccess.Application.DTOs.Setup;

namespace DO.OneAccess.Client.Services.Api;

public interface ISetupApiClient
{
    Task<SetupStatusDto> GetStatusAsync(CancellationToken ct = default);
    Task<FirstRunSetupResultDto> SetupAsync(FirstRunSetupDto dto, string bootstrapToken, CancellationToken ct = default);
}
