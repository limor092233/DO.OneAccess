using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces.Persistence;

public interface IAuditLogRepository
{
    void Add(AuditLog auditLog);
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        Guid? userId,
        string? action,
        string? entityName,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
