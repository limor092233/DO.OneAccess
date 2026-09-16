using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Application.Common.Security;
using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Audit;
using DO.OneAccess.Application.DTOs.Sections;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Services;

public class SectionService : ISectionService
{
    private readonly ISectionRepository _sectionRepository;
    private readonly IDivisionRepository _divisionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IApplicationDbContext _context;

    public SectionService(
        ISectionRepository sectionRepository,
        IDivisionRepository divisionRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IApplicationDbContext context)
    {
        _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
        _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<SectionDto>> GetSectionsAsync(
        SectionQueryDto query,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var adminDivisionId = await AuthorizationHelper.RequireAdminOrAboveScopeAsync(
            _context, actorUserId, mustBeActive: false, cancellationToken);

        if (adminDivisionId.HasValue && query.DivisionId.HasValue && query.DivisionId.Value != adminDivisionId.Value)
        {
            throw new ForbiddenException("Operation is outside administrator's assigned division scope.");
        }

        var targetDivisionId = adminDivisionId ?? query.DivisionId;

        var sections = await _sectionRepository.GetListAsync(targetDivisionId, query.IsActive, cancellationToken);

        return sections.Select(s => new SectionDto
        {
            SectionId = s.SectionId,
            DivisionId = s.DivisionId,
            DivisionName = s.Division.Name,
            Code = s.Code,
            Name = s.Name,
            Description = s.Description,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        }).ToList();
    }

    public async Task<SectionDto> GetSectionByIdAsync(
        int sectionId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.ValidateSectionScopeAsync(
            _context, actorUserId, sectionId, mustBeActive: false, cancellationToken);

        var section = await _sectionRepository.GetByIdAsync(sectionId, includeDivision: true, cancellationToken);

        if (section == null)
        {
            throw new NotFoundException(nameof(Section), sectionId);
        }

        return new SectionDto
        {
            SectionId = section.SectionId,
            DivisionId = section.DivisionId,
            DivisionName = section.Division.Name,
            Code = section.Code,
            Name = section.Name,
            Description = section.Description,
            IsActive = section.IsActive,
            CreatedAt = section.CreatedAt
        };
    }

    public async Task<SectionDto> CreateSectionAsync(
        CreateSectionDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        // Administrator creating a Section with a DivisionId outside their own Division must receive 403
        await AuthorizationHelper.ValidateDivisionScopeAsync(
            _context, actorUserId, dto.DivisionId, mustBeActive: true, cancellationToken);

        var division = await _divisionRepository.GetByIdAsync(dto.DivisionId, cancellationToken);

        if (division == null)
        {
            throw new NotFoundException(nameof(Division), dto.DivisionId);
        }

        if (string.IsNullOrWhiteSpace(dto.Code))
        {
            throw new ValidationException(nameof(dto.Code), "Code is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ValidationException(nameof(dto.Name), "Name is required.");
        }

        var exists = await _sectionRepository.ExistsByCodeAsync(dto.DivisionId, dto.Code, cancellationToken: cancellationToken);

        if (exists)
        {
            throw new ConflictException($"A section with code '{dto.Code}' already exists in division '{division.Name}'.");
        }

        var section = new Section
        {
            DivisionId = dto.DivisionId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUserId
        };

        _sectionRepository.Add(section);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "CREATE_SECTION",
            EntityName = nameof(Section),
            EntityId = section.SectionId.ToString(),
            NewValues = $"{{\"DivisionId\":{section.DivisionId},\"Code\":\"{section.Code}\",\"Name\":\"{section.Name}\"}}"
        }, cancellationToken);

        return new SectionDto
        {
            SectionId = section.SectionId,
            DivisionId = section.DivisionId,
            DivisionName = division.Name,
            Code = section.Code,
            Name = section.Name,
            Description = section.Description,
            IsActive = section.IsActive,
            CreatedAt = section.CreatedAt
        };
    }

    public async Task<SectionDto> UpdateSectionAsync(
        int sectionId,
        UpdateSectionDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.ValidateSectionScopeAsync(
            _context, actorUserId, sectionId, mustBeActive: true, cancellationToken);

        var section = await _sectionRepository.GetByIdAsync(sectionId, includeDivision: true, cancellationToken);

        if (section == null)
        {
            throw new NotFoundException(nameof(Section), sectionId);
        }

        var oldValues = $"{{\"Name\":\"{section.Name}\",\"Description\":\"{section.Description}\",\"IsActive\":{section.IsActive.ToString().ToLowerInvariant()}}}";

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            section.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            section.Description = dto.Description;
        }

        if (dto.IsActive.HasValue)
        {
            section.IsActive = dto.IsActive.Value;
        }

        section.UpdatedAt = DateTime.UtcNow;
        section.UpdatedBy = actorUserId;

        _sectionRepository.Update(section);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var newValues = $"{{\"Name\":\"{section.Name}\",\"Description\":\"{section.Description}\",\"IsActive\":{section.IsActive.ToString().ToLowerInvariant()}}}";

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "UPDATE_SECTION",
            EntityName = nameof(Section),
            EntityId = section.SectionId.ToString(),
            OldValues = oldValues,
            NewValues = newValues
        }, cancellationToken);

        return new SectionDto
        {
            SectionId = section.SectionId,
            DivisionId = section.DivisionId,
            DivisionName = section.Division.Name,
            Code = section.Code,
            Name = section.Name,
            Description = section.Description,
            IsActive = section.IsActive,
            CreatedAt = section.CreatedAt
        };
    }

    public async Task DeactivateSectionAsync(
        int sectionId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.ValidateSectionScopeAsync(
            _context, actorUserId, sectionId, mustBeActive: true, cancellationToken);

        var section = await _sectionRepository.GetByIdAsync(sectionId, includeDivision: false, cancellationToken);

        if (section == null)
        {
            throw new NotFoundException(nameof(Section), sectionId);
        }

        var oldValues = $"{{\"IsActive\":{section.IsActive.ToString().ToLowerInvariant()}}}";

        section.IsActive = false;
        section.UpdatedAt = DateTime.UtcNow;
        section.UpdatedBy = actorUserId;

        _sectionRepository.Update(section);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "DEACTIVATE_SECTION",
            EntityName = nameof(Section),
            EntityId = section.SectionId.ToString(),
            OldValues = oldValues,
            NewValues = "{\"IsActive\":false}"
        }, cancellationToken);
    }
}
