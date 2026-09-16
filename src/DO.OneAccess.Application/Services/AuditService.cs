using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Audit;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Services;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;

    public AuditService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(WriteAuditLogDto dto, CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            UserId = dto.UserId,
            Action = dto.Action,
            EntityName = dto.EntityName,
            EntityId = dto.EntityId,
            OldValues = dto.OldValues,
            NewValues = dto.NewValues,
            IpAddress = dto.IpAddress,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryDto query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.AuditLogs.AsNoTracking();

        if (query.UserId.HasValue)
        {
            dbQuery = dbQuery.Where(l => l.UserId == query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            dbQuery = dbQuery.Where(l => l.Action == query.Action);
        }

        if (query.From.HasValue)
        {
            dbQuery = dbQuery.Where(l => l.CreatedAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            dbQuery = dbQuery.Where(l => l.CreatedAt <= query.To.Value);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var items = await dbQuery
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogDto
            {
                AuditLogId = l.AuditLogId,
                UserId = l.UserId,
                Action = l.Action,
                EntityName = l.EntityName,
                EntityId = l.EntityId,
                OldValues = l.OldValues,
                NewValues = l.NewValues,
                IpAddress = l.IpAddress,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
