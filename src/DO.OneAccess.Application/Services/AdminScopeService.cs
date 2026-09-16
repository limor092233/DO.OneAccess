using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.Common.Security;
using DO.OneAccess.Application.DTOs.Access;
using DO.OneAccess.Application.DTOs.Audit;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Domain.Enums;

namespace DO.OneAccess.Application.Services;

public class AdminScopeService : IAdminScopeService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public AdminScopeService(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<AdminScopeDto>> GetAllScopesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AdministratorScopes
            .Include(s => s.Division)
            .AsNoTracking()
            .Select(s => new AdminScopeDto
            {
                AdministratorScopeId = s.AdministratorScopeId,
                AdministratorUserId = s.AdministratorUserId,
                DivisionId = s.DivisionId,
                DivisionName = s.Division.Name,
                CreatedAt = s.CreatedAt,
                CreatedBy = s.CreatedBy
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminScopeDto> AssignDivisionScopeAsync(
        AssignAdminScopeDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.RequireSystemAdministratorAsync(_context, actorUserId, mustBeActive: true, cancellationToken);

        var targetUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == dto.AdministratorUserId, cancellationToken);

        if (targetUser == null)
        {
            throw new NotFoundException(nameof(User), dto.AdministratorUserId);
        }

        var division = await _context.Divisions
            .FirstOrDefaultAsync(d => d.DivisionId == dto.DivisionId, cancellationToken);

        if (division == null)
        {
            throw new NotFoundException(nameof(Division), dto.DivisionId);
        }

        var existingScope = await _context.AdministratorScopes
            .FirstOrDefaultAsync(s => s.AdministratorUserId == dto.AdministratorUserId, cancellationToken);

        if (existingScope != null)
        {
            throw new ConflictException("Administrator already has an assigned division scope.");
        }

        var now = DateTime.UtcNow;
        var newScope = new AdministratorScope
        {
            AdministratorUserId = dto.AdministratorUserId,
            DivisionId = dto.DivisionId,
            CreatedAt = now,
            CreatedBy = actorUserId
        };

        _context.AdministratorScopes.Add(newScope);

        // Atomically revoke active refresh tokens for the affected administrator
        var activeTokensOnAssign = await _context.RefreshTokens
            .Where(t => t.UserId == dto.AdministratorUserId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokensOnAssign)
        {
            token.RevokedAt = now;
            token.RevokedByIp = "ADMIN_SCOPE_ASSIGNED";
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "ASSIGN_ADMIN_SCOPE",
            EntityName = nameof(AdministratorScope),
            EntityId = newScope.AdministratorScopeId.ToString(),
            NewValues = $"{{\"AdministratorUserId\":\"{dto.AdministratorUserId}\",\"DivisionId\":{dto.DivisionId}}}"
        }, cancellationToken);

        return new AdminScopeDto
        {
            AdministratorScopeId = newScope.AdministratorScopeId,
            AdministratorUserId = newScope.AdministratorUserId,
            DivisionId = newScope.DivisionId,
            DivisionName = division.Name,
            CreatedAt = newScope.CreatedAt,
            CreatedBy = newScope.CreatedBy
        };
    }

    public async Task RevokeDivisionScopeAsync(long adminScopeId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.RequireSystemAdministratorAsync(_context, actorUserId, mustBeActive: true, cancellationToken);

        var scope = await _context.AdministratorScopes
            .FirstOrDefaultAsync(s => s.AdministratorScopeId == adminScopeId, cancellationToken);

        if (scope == null)
        {
            throw new NotFoundException(nameof(AdministratorScope), adminScopeId);
        }

        var affectedUserId = scope.AdministratorUserId;
        var oldValues = $"{{\"AdministratorUserId\":\"{affectedUserId}\",\"DivisionId\":{scope.DivisionId}}}";

        _context.AdministratorScopes.Remove(scope);

        // Atomically revoke active refresh tokens for the affected administrator
        var now = DateTime.UtcNow;
        var activeTokensOnRevoke = await _context.RefreshTokens
            .Where(t => t.UserId == affectedUserId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokensOnRevoke)
        {
            token.RevokedAt = now;
            token.RevokedByIp = "ADMIN_SCOPE_REVOKED";
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "REVOKE_ADMIN_SCOPE",
            EntityName = nameof(AdministratorScope),
            EntityId = adminScopeId.ToString(),
            OldValues = oldValues
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminSystemAccessDto>> GetAllAdminSystemAccessGrantsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AdministratorSystemAccess
            .Include(a => a.System)
            .AsNoTracking()
            .Select(a => new AdminSystemAccessDto
            {
                AdministratorSystemAccessId = a.AdministratorSystemAccessId,
                AdministratorUserId = a.AdministratorUserId,
                SystemId = a.SystemId,
                SystemName = a.System.SystemName,
                CreatedAt = a.CreatedAt,
                CreatedBy = a.CreatedBy
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminSystemAccessDto> GrantAdminSystemAccessAsync(
        GrantAdminSystemAccessDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.RequireSystemAdministratorAsync(_context, actorUserId, mustBeActive: true, cancellationToken);

        var targetUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == dto.AdministratorUserId, cancellationToken);

        if (targetUser == null)
        {
            throw new NotFoundException(nameof(User), dto.AdministratorUserId);
        }

        var system = await _context.Systems
            .FirstOrDefaultAsync(s => s.SystemId == dto.SystemId, cancellationToken);

        if (system == null)
        {
            throw new NotFoundException(nameof(Domain.Entities.System), dto.SystemId);
        }

        var existingGrant = await _context.AdministratorSystemAccess
            .FirstOrDefaultAsync(a => a.AdministratorUserId == dto.AdministratorUserId && a.SystemId == dto.SystemId, cancellationToken);

        if (existingGrant != null)
        {
            throw new ConflictException("Administrator already has management access to this system.");
        }

        var newGrant = new AdministratorSystemAccess
        {
            AdministratorUserId = dto.AdministratorUserId,
            SystemId = dto.SystemId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUserId
        };

        _context.AdministratorSystemAccess.Add(newGrant);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "GRANT_ADMIN_SYSTEM_ACCESS",
            EntityName = nameof(AdministratorSystemAccess),
            EntityId = newGrant.AdministratorSystemAccessId.ToString(),
            NewValues = $"{{\"AdministratorUserId\":\"{dto.AdministratorUserId}\",\"SystemId\":\"{dto.SystemId}\"}}"
        }, cancellationToken);

        return new AdminSystemAccessDto
        {
            AdministratorSystemAccessId = newGrant.AdministratorSystemAccessId,
            AdministratorUserId = newGrant.AdministratorUserId,
            SystemId = newGrant.SystemId,
            SystemName = system.SystemName,
            CreatedAt = newGrant.CreatedAt,
            CreatedBy = newGrant.CreatedBy
        };
    }

    public async Task RevokeAdminSystemAccessAsync(long adminSystemAccessId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.RequireSystemAdministratorAsync(_context, actorUserId, mustBeActive: true, cancellationToken);

        var grant = await _context.AdministratorSystemAccess
            .FirstOrDefaultAsync(a => a.AdministratorSystemAccessId == adminSystemAccessId, cancellationToken);

        if (grant == null)
        {
            throw new NotFoundException(nameof(AdministratorSystemAccess), adminSystemAccessId);
        }

        var oldValues = $"{{\"AdministratorUserId\":\"{grant.AdministratorUserId}\",\"SystemId\":\"{grant.SystemId}\"}}";

        _context.AdministratorSystemAccess.Remove(grant);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "REVOKE_ADMIN_SYSTEM_ACCESS",
            EntityName = nameof(AdministratorSystemAccess),
            EntityId = adminSystemAccessId.ToString(),
            OldValues = oldValues
        }, cancellationToken);
    }
}
