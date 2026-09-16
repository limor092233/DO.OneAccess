using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Repositories;

public class LoginHistoryRepository : ILoginHistoryRepository
{
    private readonly AppDbContext _context;

    public LoginHistoryRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(LoginHistory loginHistory)
    {
        _context.LoginHistories.Add(loginHistory);
    }

    public async Task<(IReadOnlyList<LoginHistory> Items, int TotalCount)> GetPagedAsync(
        Guid? userId,
        bool? success,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LoginHistories
            .Include(l => l.User)
            .AsNoTracking();

        if (userId.HasValue)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }

        if (success.HasValue)
        {
            query = query.Where(l => l.Success == success.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var p = page <= 0 ? 1 : page;
        var ps = pageSize <= 0 ? 20 : pageSize;

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
