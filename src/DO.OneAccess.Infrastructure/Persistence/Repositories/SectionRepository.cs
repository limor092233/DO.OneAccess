using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Repositories;

public class SectionRepository : ISectionRepository
{
    private readonly AppDbContext _context;

    public SectionRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Section?> GetByIdAsync(int sectionId, bool includeDivision = false, CancellationToken cancellationToken = default)
    {
        if (!includeDivision)
        {
            return await _context.Sections
                .FirstOrDefaultAsync(s => s.SectionId == sectionId, cancellationToken);
        }

        return await _context.Sections
            .Include(s => s.Division)
            .FirstOrDefaultAsync(s => s.SectionId == sectionId, cancellationToken);
    }

    public async Task<Section?> GetByCodeAsync(int divisionId, string code, CancellationToken cancellationToken = default)
    {
        var trimmed = code.Trim();
        return await _context.Sections
            .Include(s => s.Division)
            .FirstOrDefaultAsync(s => s.DivisionId == divisionId && s.Code == trimmed, cancellationToken);
    }

    public async Task<IReadOnlyList<Section>> GetByDivisionIdAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        return await _context.Sections
            .Include(s => s.Division)
            .Where(s => s.DivisionId == divisionId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Section>> GetListAsync(int? divisionId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = _context.Sections
            .Include(s => s.Division)
            .AsNoTracking();

        if (divisionId.HasValue)
        {
            query = query.Where(s => s.DivisionId == divisionId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(int divisionId, string code, int? excludeSectionId = null, CancellationToken cancellationToken = default)
    {
        var trimmed = code.Trim();
        var query = _context.Sections.Where(s => s.DivisionId == divisionId && s.Code == trimmed);

        if (excludeSectionId.HasValue)
        {
            query = query.Where(s => s.SectionId != excludeSectionId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public void Add(Section section)
    {
        _context.Sections.Add(section);
    }

    public void Update(Section section)
    {
        _context.Sections.Update(section);
    }
}
