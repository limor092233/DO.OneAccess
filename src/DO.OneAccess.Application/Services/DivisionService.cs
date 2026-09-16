using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.Common.Security;
using DO.OneAccess.Application.DTOs.Audit;
using DO.OneAccess.Application.DTOs.Divisions;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Services;

public class DivisionService : IDivisionService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public DivisionService(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<DivisionDto>> GetAllDivisionsAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var adminDivisionId = await AuthorizationHelper.RequireAdminOrAboveScopeAsync(
            _context, actorUserId, mustBeActive: false, cancellationToken);

        var query = _context.Divisions
            .AsNoTracking();

        if (adminDivisionId.HasValue)
        {
            query = query.Where(d => d.DivisionId == adminDivisionId.Value);
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

    public async Task<DivisionDto> GetDivisionByIdAsync(
        int divisionId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        // Administrator requesting another Division must receive 403
        await AuthorizationHelper.ValidateDivisionScopeAsync(
            _context, actorUserId, divisionId, mustBeActive: false, cancellationToken);

        var division = await _context.Divisions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DivisionId == divisionId, cancellationToken);

        if (division == null)
        {
            throw new NotFoundException(nameof(Division), divisionId);
        }

        return new DivisionDto
        {
            DivisionId = division.DivisionId,
            Code = division.Code,
            Name = division.Name,
            Description = division.Description,
            IsActive = division.IsActive,
            CreatedAt = division.CreatedAt
        };
    }

    public async Task<DivisionDto> CreateDivisionAsync(
        CreateDivisionDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.RequireSystemAdministratorAsync(
            _context, actorUserId, mustBeActive: true, cancellationToken);

        if (string.IsNullOrWhiteSpace(dto.Code))
        {
            throw new ValidationException(nameof(dto.Code), "Code is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ValidationException(nameof(dto.Name), "Name is required.");
        }

        var exists = await _context.Divisions
            .AnyAsync(d => d.Code == dto.Code, cancellationToken);

        if (exists)
        {
            throw new ConflictException($"A division with code '{dto.Code}' already exists.");
        }

        var division = new Division
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUserId
        };

        _context.Divisions.Add(division);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "CREATE_DIVISION",
            EntityName = nameof(Division),
            EntityId = division.DivisionId.ToString(),
            NewValues = $"{{\"Code\":\"{division.Code}\",\"Name\":\"{division.Name}\"}}"
        }, cancellationToken);

        return new DivisionDto
        {
            DivisionId = division.DivisionId,
            Code = division.Code,
            Name = division.Name,
            Description = division.Description,
            IsActive = division.IsActive,
            CreatedAt = division.CreatedAt
        };
    }

    public async Task<DivisionDto> UpdateDivisionAsync(
        int divisionId,
        UpdateDivisionDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.RequireSystemAdministratorAsync(
            _context, actorUserId, mustBeActive: true, cancellationToken);

        var division = await _context.Divisions
            .FirstOrDefaultAsync(d => d.DivisionId == divisionId, cancellationToken);

        if (division == null)
        {
            throw new NotFoundException(nameof(Division), divisionId);
        }

        var oldValues = $"{{\"Name\":\"{division.Name}\",\"Description\":\"{division.Description}\",\"IsActive\":{division.IsActive.ToString().ToLowerInvariant()}}}";

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            division.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            division.Description = dto.Description;
        }

        if (dto.IsActive.HasValue)
        {
            division.IsActive = dto.IsActive.Value;
        }

        division.UpdatedAt = DateTime.UtcNow;
        division.UpdatedBy = actorUserId;

        await _context.SaveChangesAsync(cancellationToken);

        var newValues = $"{{\"Name\":\"{division.Name}\",\"Description\":\"{division.Description}\",\"IsActive\":{division.IsActive.ToString().ToLowerInvariant()}}}";

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "UPDATE_DIVISION",
            EntityName = nameof(Division),
            EntityId = division.DivisionId.ToString(),
            OldValues = oldValues,
            NewValues = newValues
        }, cancellationToken);

        return new DivisionDto
        {
            DivisionId = division.DivisionId,
            Code = division.Code,
            Name = division.Name,
            Description = division.Description,
            IsActive = division.IsActive,
            CreatedAt = division.CreatedAt
        };
    }

    public async Task DeactivateDivisionAsync(
        int divisionId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.RequireSystemAdministratorAsync(
            _context, actorUserId, mustBeActive: true, cancellationToken);

        var division = await _context.Divisions
            .FirstOrDefaultAsync(d => d.DivisionId == divisionId, cancellationToken);

        if (division == null)
        {
            throw new NotFoundException(nameof(Division), divisionId);
        }

        var oldValues = $"{{\"IsActive\":{division.IsActive.ToString().ToLowerInvariant()}}}";

        division.IsActive = false;
        division.UpdatedAt = DateTime.UtcNow;
        division.UpdatedBy = actorUserId;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "DEACTIVATE_DIVISION",
            EntityName = nameof(Division),
            EntityId = division.DivisionId.ToString(),
            OldValues = oldValues,
            NewValues = "{\"IsActive\":false}"
        }, cancellationToken);
    }
}
