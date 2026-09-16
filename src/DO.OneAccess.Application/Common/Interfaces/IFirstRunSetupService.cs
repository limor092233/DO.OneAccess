using DO.OneAccess.Application.DTOs.Setup;

namespace DO.OneAccess.Application.Common.Interfaces;

public interface IFirstRunSetupService
{
    Task<SetupStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<FirstRunSetupResultDto> SetupAsync(
        FirstRunSetupDto dto,
        string? clientIp,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
