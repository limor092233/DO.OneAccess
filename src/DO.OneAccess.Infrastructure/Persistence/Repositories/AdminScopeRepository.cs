using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence.Repositories;

public class AdminScopeRepository : IAdminScopeRepository
{
    private readonly AppDbContext _context;

    public AdminScopeRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<AdministratorScope?> GetByIdAsync(int administratorScopeId, CancellationToken cancellationToken = default)
    {
        return await _context.AdministratorScopes
            .Include(s => s.AdministratorUser)
            .Include(s => s.Division)
            .FirstOrDefaultAsync(s => s.AdministratorScopeId == administratorScopeId, cancellationToken);
    }

    public async Task<AdministratorScope?> GetByAdminUserIdAsync(Guid administratorUserId, CancellationToken cancellationToken = default)
    {
        return await _context.AdministratorScopes
            .Include(s => s.Division)
            .FirstOrDefaultAsync(s => s.AdministratorUserId == administratorUserId, cancellationToken);
    }

    public async Task<IReadOnlyList<AdministratorScope>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AdministratorScopes
            .Include(s => s.AdministratorUser)
            .Include(s => s.Division)
            .OrderBy(s => s.AdministratorUser.Username)
            .ToListAsync(cancellationToken);
    }

    public void Add(AdministratorScope scope)
    {
        _context.AdministratorScopes.Add(scope);
    }

    public void Remove(AdministratorScope scope)
    {
        _context.AdministratorScopes.Remove(scope);
    }

    public async Task RemoveByAdminUserIdAsync(Guid administratorUserId, CancellationToken cancellationToken = default)
    {
        var scopes = await _context.AdministratorScopes
            .Where(s => s.AdministratorUserId == administratorUserId)
            .ToListAsync(cancellationToken);

        if (scopes.Count > 0)
        {
            _context.AdministratorScopes.RemoveRange(scopes);
        }
    }
}
