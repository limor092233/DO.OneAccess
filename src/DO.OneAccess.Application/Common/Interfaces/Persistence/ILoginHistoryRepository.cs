using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces.Persistence;

public interface ILoginHistoryRepository
{
    void Add(LoginHistory loginHistory);
    Task<(IReadOnlyList<LoginHistory> Items, int TotalCount)> GetPagedAsync(
        Guid? userId,
        bool? success,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
