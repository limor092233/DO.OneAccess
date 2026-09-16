using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.Common.Security;
using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Access;
using DO.OneAccess.Application.DTOs.Audit;
using DO.OneAccess.Application.DTOs.Systems;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Domain.Enums;

namespace DO.OneAccess.Application.Services;

public class SystemAccessService : ISystemAccessService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public SystemAccessService(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<SystemAccessResult> ResolveAccessAsync(
        Guid userId,
        Guid systemId,
        CancellationToken cancellationToken = default)
    {
        // Step 1: User active check
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null || !user.IsActive)
        {
            return new SystemAccessResult(false, "User is inactive or not found.");
        }

        // Step 2: System active check
        var system = await _context.Systems
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SystemId == systemId, cancellationToken);

        if (system == null || !system.IsActive)
        {
            return new SystemAccessResult(false, "System is inactive or not found.");
        }

        // Step 3: Explicit Allow/Deny override
        var overrideRecord = await _context.UserSystemAccess
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId && a.SystemId == systemId, cancellationToken);

        if (overrideRecord != null)
        {
            if (string.Equals(overrideRecord.AccessType, nameof(AccessOverride.Allow), StringComparison.OrdinalIgnoreCase))
            {
                return new SystemAccessResult(true, "Access granted via explicit Allow override.");
            }

            if (string.Equals(overrideRecord.AccessType, nameof(AccessOverride.Deny), StringComparison.OrdinalIgnoreCase))
            {
                return new SystemAccessResult(false, "Access denied via explicit Deny override.");
            }
        }

        // Step 4: System DefaultAccess
        if (string.Equals(system.DefaultAccess, nameof(DefaultAccess.All), StringComparison.OrdinalIgnoreCase))
        {
            return new SystemAccessResult(true, "Access granted via system DefaultAccess 'All'.");
        }

        return new SystemAccessResult(false, "Access denied via system DefaultAccess 'Restricted'.");
    }

    public async Task<IReadOnlyList<SystemDto>> GetUserAccessibleSystemsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null || !user.IsActive)
        {
            return Array.Empty<SystemDto>();
        }

        var activeSystems = await _context.Systems
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        var overrides = await _context.UserSystemAccess
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToDictionaryAsync(a => a.SystemId, a => a.AccessType, cancellationToken);

        var accessibleSystems = new List<SystemDto>();

        foreach (var sys in activeSystems)
        {
            var isAccessible = false;

            if (overrides.TryGetValue(sys.SystemId, out var accessType))
            {
                if (string.Equals(accessType, nameof(AccessOverride.Allow), StringComparison.OrdinalIgnoreCase))
                {
                    isAccessible = true;
                }
                // If Deny, isAccessible remains false
            }
            else
            {
                if (string.Equals(sys.DefaultAccess, nameof(DefaultAccess.All), StringComparison.OrdinalIgnoreCase))
                {
                    isAccessible = true;
                }
            }

            if (isAccessible)
            {
                accessibleSystems.Add(new SystemDto
                {
                    SystemId = sys.SystemId,
                    SystemCode = sys.SystemCode,
                    SystemName = sys.SystemName,
                    Description = sys.Description,
                    BaseUrl = sys.BaseUrl,
                    IconUrl = sys.IconUrl,
                    DefaultAccess = sys.DefaultAccess,
                    IsActive = sys.IsActive,
                    CreatedAt = sys.CreatedAt
                });
            }
        }

        return accessibleSystems;
    }

    public async Task<PagedResult<UserSystemAccessDto>> GetUserSystemAccessOverridesAsync(
        Guid actorUserId,
        Guid? filterUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var adminDivisionId = await AuthorizationHelper.RequireAdminOrAboveScopeAsync(
            _context, actorUserId, mustBeActive: false, cancellationToken);

        var dbQuery = _context.UserSystemAccess
            .Include(a => a.System)
            .Include(a => a.User)
                .ThenInclude(u => u.Section)
            .AsNoTracking();

        if (adminDivisionId.HasValue)
        {
            dbQuery = dbQuery.Where(a => a.User.Section != null && a.User.Section.DivisionId == adminDivisionId.Value);
        }

        if (filterUserId.HasValue)
        {
            if (adminDivisionId.HasValue)
            {
                await AuthorizationHelper.ValidateUserScopeAsync(
                    _context, actorUserId, filterUserId.Value, mustBeActive: false, cancellationToken);
            }

            dbQuery = dbQuery.Where(a => a.UserId == filterUserId.Value);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var p = page <= 0 ? 1 : page;
        var ps = pageSize <= 0 ? 20 : pageSize;

        var items = await dbQuery
            .OrderByDescending(a => a.CreatedAt)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(a => new UserSystemAccessDto
            {
                UserSystemAccessId = a.UserSystemAccessId,
                UserId = a.UserId,
                SystemId = a.SystemId,
                SystemName = a.System.SystemName,
                AccessType = a.AccessType,
                CreatedAt = a.CreatedAt,
                CreatedBy = a.CreatedBy,
                UpdatedAt = a.UpdatedAt,
                UpdatedBy = a.UpdatedBy
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<UserSystemAccessDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = p,
            PageSize = ps
        };
    }

    public async Task<UserSystemAccessDto> SetUserSystemAccessAsync(
        SetUserSystemAccessDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        // Re-verify actor is active (rule: inactive actors must be re-verified from DB for protected state-mutating operations)
        await AuthorizationHelper.GetAndValidateActorAsync(_context, actorUserId, mustBeActive: true, cancellationToken);

        // Normalize and validate AccessType
        string canonicalAccessType;
        if (string.Equals(dto.AccessType, nameof(AccessOverride.Allow), StringComparison.OrdinalIgnoreCase))
        {
            canonicalAccessType = nameof(AccessOverride.Allow);
        }
        else if (string.Equals(dto.AccessType, nameof(AccessOverride.Deny), StringComparison.OrdinalIgnoreCase))
        {
            canonicalAccessType = nameof(AccessOverride.Deny);
        }
        else
        {
            throw new ValidationException(nameof(dto.AccessType), "AccessType must be 'Allow' or 'Deny'.");
        }

        // Verify target user exists and is within actor's division scope
        await AuthorizationHelper.ValidateUserScopeAsync(_context, actorUserId, dto.UserId, mustBeActive: true, cancellationToken);

        // Verify target system exists and is within actor's system-management scope
        var system = await _context.Systems
            .FirstOrDefaultAsync(s => s.SystemId == dto.SystemId, cancellationToken);

        if (system == null)
        {
            throw new NotFoundException(nameof(Domain.Entities.System), dto.SystemId);
        }

        await AuthorizationHelper.ValidateAdminSystemAccessAsync(_context, actorUserId, dto.SystemId, mustBeActive: true, cancellationToken);

        // UPSERT behavior: existing (UserId, SystemId) updates AccessType; do not return ConflictException merely because record exists
        var existing = await _context.UserSystemAccess
            .FirstOrDefaultAsync(a => a.UserId == dto.UserId && a.SystemId == dto.SystemId, cancellationToken);

        if (existing != null)
        {
            var oldValues = $"{{\"AccessType\":\"{existing.AccessType}\"}}";
            existing.AccessType = canonicalAccessType;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = actorUserId;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(new WriteAuditLogDto
            {
                UserId = actorUserId,
                Action = "UPDATE_USER_SYSTEM_ACCESS",
                EntityName = nameof(UserSystemAccess),
                EntityId = existing.UserSystemAccessId.ToString(),
                OldValues = oldValues,
                NewValues = $"{{\"AccessType\":\"{canonicalAccessType}\"}}"
            }, cancellationToken);

            return new UserSystemAccessDto
            {
                UserSystemAccessId = existing.UserSystemAccessId,
                UserId = existing.UserId,
                SystemId = existing.SystemId,
                SystemName = system.SystemName,
                AccessType = existing.AccessType,
                CreatedAt = existing.CreatedAt,
                CreatedBy = existing.CreatedBy,
                UpdatedAt = existing.UpdatedAt,
                UpdatedBy = existing.UpdatedBy
            };
        }
        else
        {
            var newAccess = new UserSystemAccess
            {
                UserId = dto.UserId,
                SystemId = dto.SystemId,
                AccessType = canonicalAccessType,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actorUserId
            };

            _context.UserSystemAccess.Add(newAccess);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(new WriteAuditLogDto
            {
                UserId = actorUserId,
                Action = "CREATE_USER_SYSTEM_ACCESS",
                EntityName = nameof(UserSystemAccess),
                EntityId = newAccess.UserSystemAccessId.ToString(),
                NewValues = $"{{\"UserId\":\"{dto.UserId}\",\"SystemId\":\"{dto.SystemId}\",\"AccessType\":\"{canonicalAccessType}\"}}"
            }, cancellationToken);

            return new UserSystemAccessDto
            {
                UserSystemAccessId = newAccess.UserSystemAccessId,
                UserId = newAccess.UserId,
                SystemId = newAccess.SystemId,
                SystemName = system.SystemName,
                AccessType = newAccess.AccessType,
                CreatedAt = newAccess.CreatedAt,
                CreatedBy = newAccess.CreatedBy
            };
        }
    }

    public async Task RevokeUserSystemAccessAsync(
        long userSystemAccessId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.GetAndValidateActorAsync(_context, actorUserId, mustBeActive: true, cancellationToken);

        var record = await _context.UserSystemAccess
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserSystemAccessId == userSystemAccessId, cancellationToken);

        if (record == null)
        {
            throw new NotFoundException(nameof(UserSystemAccess), userSystemAccessId);
        }

        await AuthorizationHelper.ValidateUserScopeAsync(_context, actorUserId, record.UserId, mustBeActive: true, cancellationToken);
        await AuthorizationHelper.ValidateAdminSystemAccessAsync(_context, actorUserId, record.SystemId, mustBeActive: true, cancellationToken);

        var oldValues = $"{{\"UserId\":\"{record.UserId}\",\"SystemId\":\"{record.SystemId}\",\"AccessType\":\"{record.AccessType}\"}}";

        _context.UserSystemAccess.Remove(record);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "REVOKE_USER_SYSTEM_ACCESS",
            EntityName = nameof(UserSystemAccess),
            EntityId = userSystemAccessId.ToString(),
            OldValues = oldValues
        }, cancellationToken);
    }
}
