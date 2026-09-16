using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Audit;

namespace DO.OneAccess.Application.Common.Interfaces;

/// <summary>
/// Audit log management. Write is internal/services, querying is restricted to System Administrators.
/// </summary>
public interface IAuditService
{
    Task LogAsync(WriteAuditLogDto dto, CancellationToken cancellationToken = default);

    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryDto query, CancellationToken cancellationToken = default);
}
