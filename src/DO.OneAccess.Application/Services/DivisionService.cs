using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Application.Common.Mappings;
using DO.OneAccess.Application.Common.Security;
using DO.OneAccess.Application.DTOs.Audit;
using DO.OneAccess.Application.DTOs.Divisions;
using DO.OneAccess.Domain.Entities;
using Mapster;

namespace DO.OneAccess.Application.Services;

public class DivisionService : IDivisionService
{
    private readonly IDivisionRepository _divisionRepository;
    private readonly IDivisionQueries _divisionQueries;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IApplicationDbContext _context;

    public DivisionService(
        IDivisionRepository divisionRepository,
        IDivisionQueries divisionQueries,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IApplicationDbContext context)
    {
        _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
        _divisionQueries = divisionQueries ?? throw new ArgumentNullException(nameof(divisionQueries));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<DivisionDto>> GetAllDivisionsAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var adminDivisionId = await AuthorizationHelper.RequireAdminOrAboveScopeAsync(
            _context, actorUserId, mustBeActive: false, cancellationToken);

        return await _divisionQueries.GetAllAsync(adminDivisionId, cancellationToken);
    }

    public async Task<DivisionDto> GetDivisionByIdAsync(
        int divisionId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        // Administrator requesting another Division must receive 403
        await AuthorizationHelper.ValidateDivisionScopeAsync(
            _context, actorUserId, divisionId, mustBeActive: false, cancellationToken);

        var division = await _divisionQueries.GetByIdAsync(divisionId, cancellationToken);

        if (division == null)
        {
            throw new NotFoundException(nameof(Division), divisionId);
        }

        return division;
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

        var exists = await _divisionRepository.ExistsByCodeAsync(dto.Code, cancellationToken: cancellationToken);

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

        _divisionRepository.Add(division);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "CREATE_DIVISION",
            EntityName = nameof(Division),
            EntityId = division.DivisionId.ToString(),
            NewValues = $"{{\"Code\":\"{division.Code}\",\"Name\":\"{division.Name}\"}}"
        }, cancellationToken);

        return division.Adapt<DivisionDto>(MappingConfig.Config);
    }

    public async Task<DivisionDto> UpdateDivisionAsync(
        int divisionId,
        UpdateDivisionDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.RequireSystemAdministratorAsync(
            _context, actorUserId, mustBeActive: true, cancellationToken);

        var division = await _divisionRepository.GetByIdAsync(divisionId, cancellationToken);

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

        _divisionRepository.Update(division);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        return division.Adapt<DivisionDto>(MappingConfig.Config);
    }

    public async Task DeactivateDivisionAsync(
        int divisionId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.RequireSystemAdministratorAsync(
            _context, actorUserId, mustBeActive: true, cancellationToken);

        var division = await _divisionRepository.GetByIdAsync(divisionId, cancellationToken);

        if (division == null)
        {
            throw new NotFoundException(nameof(Division), divisionId);
        }

        var oldValues = $"{{\"IsActive\":{division.IsActive.ToString().ToLowerInvariant()}}}";

        division.IsActive = false;
        division.UpdatedAt = DateTime.UtcNow;
        division.UpdatedBy = actorUserId;

        _divisionRepository.Update(division);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
