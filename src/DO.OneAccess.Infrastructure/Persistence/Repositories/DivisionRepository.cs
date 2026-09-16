using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Repositories;

public class DivisionRepository : IDivisionRepository
{
    private readonly AppDbContext _context;

    public DivisionRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Division?> GetByIdAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        return await _context.Divisions
            .FirstOrDefaultAsync(d => d.DivisionId == divisionId, cancellationToken);
    }

    public async Task<Division?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var trimmed = code.Trim();
        return await _context.Divisions
            .FirstOrDefaultAsync(d => d.Code == trimmed, cancellationToken);
    }

    public async Task<IReadOnlyList<Division>> GetAllAsync(int? scopedDivisionId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Divisions.AsQueryable();

        if (scopedDivisionId.HasValue)
        {
            query = query.Where(d => d.DivisionId == scopedDivisionId.Value);
        }

        return await query
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, int? excludeDivisionId = null, CancellationToken cancellationToken = default)
    {
        var trimmed = code.Trim();
        var query = _context.Divisions.Where(d => d.Code == trimmed);

        if (excludeDivisionId.HasValue)
        {
            query = query.Where(d => d.DivisionId != excludeDivisionId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public void Add(Division division)
    {
        _context.Divisions.Add(division);
    }

    public void Update(Division division)
    {
        _context.Divisions.Update(division);
    }
}
