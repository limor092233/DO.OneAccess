using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Audit;

namespace DO.OneAccess.Client.Services.Api;

public interface IAuditApiClient
{
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryDto query, CancellationToken ct = default);
    Task<PagedResult<LoginHistoryDto>> GetLoginHistoriesAsync(LoginHistoryQueryDto query, CancellationToken ct = default);
}
