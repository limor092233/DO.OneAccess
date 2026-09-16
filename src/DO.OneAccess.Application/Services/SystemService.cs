using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.Common.Security;
using DO.OneAccess.Application.DTOs.Audit;
using DO.OneAccess.Application.DTOs.Systems;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Domain.Enums;

namespace DO.OneAccess.Application.Services;

public class SystemService : ISystemService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public SystemService(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<SystemDto>> GetRegisteredSystemsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Systems
            .AsNoTracking()
            .OrderBy(s => s.SystemName)
            .Select(s => new SystemDto
            {
                SystemId = s.SystemId,
                SystemCode = s.SystemCode,
                SystemName = s.SystemName,
                Description = s.Description,
                BaseUrl = s.BaseUrl,
                IconUrl = s.IconUrl,
                DefaultAccess = s.DefaultAccess,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SystemDto> GetSystemByIdAsync(
        Guid systemId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        // Administrator cannot manage Systems without AdministratorSystemAccess.
        await AuthorizationHelper.ValidateAdminSystemAccessAsync(
            _context, actorUserId, systemId, mustBeActive: false, cancellationToken);

        var system = await _context.Systems
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SystemId == systemId, cancellationToken);

        if (system == null)
        {
            throw new NotFoundException(nameof(Domain.Entities.System), systemId);
        }

        return new SystemDto
        {
            SystemId = system.SystemId,
            SystemCode = system.SystemCode,
            SystemName = system.SystemName,
            Description = system.Description,
            BaseUrl = system.BaseUrl,
            IconUrl = system.IconUrl,
            DefaultAccess = system.DefaultAccess,
            IsActive = system.IsActive,
            CreatedAt = system.CreatedAt
        };
    }

    public async Task<SystemDto> RegisterSystemAsync(
        RegisterSystemDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.RequireSystemAdministratorAsync(
            _context, actorUserId, mustBeActive: true, cancellationToken);

        if (string.IsNullOrWhiteSpace(dto.SystemCode))
        {
            throw new ValidationException(nameof(dto.SystemCode), "SystemCode is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.SystemName))
        {
            throw new ValidationException(nameof(dto.SystemName), "SystemName is required.");
        }

        string canonicalDefaultAccess;
        if (string.Equals(dto.DefaultAccess, nameof(DefaultAccess.All), StringComparison.OrdinalIgnoreCase))
        {
            canonicalDefaultAccess = nameof(DefaultAccess.All);
        }
        else if (string.Equals(dto.DefaultAccess, nameof(DefaultAccess.Restricted), StringComparison.OrdinalIgnoreCase))
        {
            canonicalDefaultAccess = nameof(DefaultAccess.Restricted);
        }
        else
        {
            throw new ValidationException(nameof(dto.DefaultAccess), "DefaultAccess must be 'All' or 'Restricted'.");
        }

        var exists = await _context.Systems
            .AnyAsync(s => s.SystemCode == dto.SystemCode, cancellationToken);

        if (exists)
        {
            throw new ConflictException($"A system with code '{dto.SystemCode}' already exists.");
        }

        var system = new Domain.Entities.System
        {
            SystemCode = dto.SystemCode,
            SystemName = dto.SystemName,
            Description = dto.Description,
            BaseUrl = dto.BaseUrl,
            IconUrl = dto.IconUrl,
            DefaultAccess = canonicalDefaultAccess,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUserId
        };

        _context.Systems.Add(system);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "REGISTER_SYSTEM",
            EntityName = nameof(Domain.Entities.System),
            EntityId = system.SystemId.ToString(),
            NewValues = $"{{\"SystemCode\":\"{system.SystemCode}\",\"SystemName\":\"{system.SystemName}\",\"DefaultAccess\":\"{system.DefaultAccess}\"}}"
        }, cancellationToken);

        return new SystemDto
        {
            SystemId = system.SystemId,
            SystemCode = system.SystemCode,
            SystemName = system.SystemName,
            Description = system.Description,
            BaseUrl = system.BaseUrl,
            IconUrl = system.IconUrl,
            DefaultAccess = system.DefaultAccess,
            IsActive = system.IsActive,
            CreatedAt = system.CreatedAt
        };
    }

    public async Task<SystemDto> UpdateSystemAsync(
        Guid systemId,
        UpdateSystemDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.RequireSystemAdministratorAsync(
            _context, actorUserId, mustBeActive: true, cancellationToken);

        var system = await _context.Systems
            .FirstOrDefaultAsync(s => s.SystemId == systemId, cancellationToken);

        if (system == null)
        {
            throw new NotFoundException(nameof(Domain.Entities.System), systemId);
        }

        var oldValues = $"{{\"SystemName\":\"{system.SystemName}\",\"DefaultAccess\":\"{system.DefaultAccess}\",\"IsActive\":{system.IsActive.ToString().ToLowerInvariant()}}}";

        if (!string.IsNullOrWhiteSpace(dto.SystemName))
        {
            system.SystemName = dto.SystemName;
        }

        if (dto.Description != null)
        {
            system.Description = dto.Description;
        }

        if (dto.BaseUrl != null)
        {
            system.BaseUrl = dto.BaseUrl;
        }

        if (dto.IconUrl != null)
        {
            system.IconUrl = dto.IconUrl;
        }

        if (!string.IsNullOrWhiteSpace(dto.DefaultAccess))
        {
            if (string.Equals(dto.DefaultAccess, nameof(DefaultAccess.All), StringComparison.OrdinalIgnoreCase))
            {
                system.DefaultAccess = nameof(DefaultAccess.All);
            }
            else if (string.Equals(dto.DefaultAccess, nameof(DefaultAccess.Restricted), StringComparison.OrdinalIgnoreCase))
            {
                system.DefaultAccess = nameof(DefaultAccess.Restricted);
            }
            else
            {
                throw new ValidationException(nameof(dto.DefaultAccess), "DefaultAccess must be 'All' or 'Restricted'.");
            }
        }

        if (dto.IsActive.HasValue)
        {
            system.IsActive = dto.IsActive.Value;
        }

        system.UpdatedAt = DateTime.UtcNow;
        system.UpdatedBy = actorUserId;

        await _context.SaveChangesAsync(cancellationToken);

        var newValues = $"{{\"SystemName\":\"{system.SystemName}\",\"DefaultAccess\":\"{system.DefaultAccess}\",\"IsActive\":{system.IsActive.ToString().ToLowerInvariant()}}}";

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "UPDATE_SYSTEM",
            EntityName = nameof(Domain.Entities.System),
            EntityId = system.SystemId.ToString(),
            OldValues = oldValues,
            NewValues = newValues
        }, cancellationToken);

        return new SystemDto
        {
            SystemId = system.SystemId,
            SystemCode = system.SystemCode,
            SystemName = system.SystemName,
            Description = system.Description,
            BaseUrl = system.BaseUrl,
            IconUrl = system.IconUrl,
            DefaultAccess = system.DefaultAccess,
            IsActive = system.IsActive,
            CreatedAt = system.CreatedAt
        };
    }

    public async Task DeactivateSystemAsync(
        Guid systemId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.RequireSystemAdministratorAsync(
            _context, actorUserId, mustBeActive: true, cancellationToken);

        var system = await _context.Systems
            .FirstOrDefaultAsync(s => s.SystemId == systemId, cancellationToken);

        if (system == null)
        {
            throw new NotFoundException(nameof(Domain.Entities.System), systemId);
        }

        var oldValues = $"{{\"IsActive\":{system.IsActive.ToString().ToLowerInvariant()}}}";

        system.IsActive = false;
        system.UpdatedAt = DateTime.UtcNow;
        system.UpdatedBy = actorUserId;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "DEACTIVATE_SYSTEM",
            EntityName = nameof(Domain.Entities.System),
            EntityId = system.SystemId.ToString(),
            OldValues = oldValues,
            NewValues = "{\"IsActive\":false}"
        }, cancellationToken);
    }
}
