using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Audit;

namespace DO.OneAccess.Application.Services;

public class LoginHistoryService : ILoginHistoryService
{
    private readonly IApplicationDbContext _context;

    public LoginHistoryService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LoginHistoryDto>> GetLoginHistoriesAsync(LoginHistoryQueryDto query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.LoginHistories.AsNoTracking();

        if (query.UserId.HasValue)
        {
            dbQuery = dbQuery.Where(h => h.UserId == query.UserId.Value);
        }

        if (query.Success.HasValue)
        {
            dbQuery = dbQuery.Where(h => h.Success == query.Success.Value);
        }

        if (query.From.HasValue)
        {
            dbQuery = dbQuery.Where(h => h.CreatedAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            dbQuery = dbQuery.Where(h => h.CreatedAt <= query.To.Value);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var items = await dbQuery
            .OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new LoginHistoryDto
            {
                LoginHistoryId = h.LoginHistoryId,
                UserId = h.UserId,
                UsernameAttempted = h.UsernameAttempted,
                Success = h.Success,
                IpAddress = h.IpAddress,
                UserAgent = h.UserAgent,
                FailureReason = h.FailureReason,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LoginHistoryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
