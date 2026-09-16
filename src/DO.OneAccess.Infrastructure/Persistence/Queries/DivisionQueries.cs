namespace DO.OneAccess.Infrastructure.Persistence.Queries;

using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Application.DTOs.Divisions;

public class DivisionQueries : IDivisionQueries
{
    private readonly AppDbContext _context;

    public DivisionQueries(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<DivisionDto>> GetAllAsync(
        int? scopedDivisionId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Divisions
            .AsNoTracking();

        if (scopedDivisionId.HasValue)
        {
            query = query.Where(d => d.DivisionId == scopedDivisionId.Value);
        }

        return await query
            .OrderBy(d => d.Name)
            .Select(d => new DivisionDto
            {
                DivisionId = d.DivisionId,
                Code = d.Code,
                Name = d.Name,
                Description = d.Description,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DivisionDto?> GetByIdAsync(
        int divisionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Divisions
            .AsNoTracking()
            .Where(d => d.DivisionId == divisionId)
            .Select(d => new DivisionDto
            {
                DivisionId = d.DivisionId,
                Code = d.Code,
                Name = d.Name,
                Description = d.Description,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
