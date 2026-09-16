using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Application.Common.Security;
using DO.OneAccess.Application.DTOs.Access;
using DO.OneAccess.Application.DTOs.Audit;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Services;

public class AdminScopeService : IAdminScopeService
{
    private readonly IAdminScopeRepository _adminScopeRepository;
    private readonly IAdminSystemAccessRepository _adminSystemAccessRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDivisionRepository _divisionRepository;
    private readonly ISystemRepository _systemRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IApplicationDbContext _context;

    public AdminScopeService(
        IAdminScopeRepository adminScopeRepository,
        IAdminSystemAccessRepository adminSystemAccessRepository,
        IUserRepository userRepository,
        IDivisionRepository divisionRepository,
        ISystemRepository systemRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IApplicationDbContext context)
    {
        _adminScopeRepository = adminScopeRepository ?? throw new ArgumentNullException(nameof(adminScopeRepository));
        _adminSystemAccessRepository = adminSystemAccessRepository ?? throw new ArgumentNullException(nameof(adminSystemAccessRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
        _systemRepository = systemRepository ?? throw new ArgumentNullException(nameof(systemRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<AdminScopeDto>> GetAllScopesAsync(CancellationToken cancellationToken = default)
    {
        var scopes = await _adminScopeRepository.GetAllAsync(cancellationToken);

        return scopes.Select(s => new AdminScopeDto
        {
            AdministratorScopeId = s.AdministratorScopeId,
            AdministratorUserId = s.AdministratorUserId,
            DivisionId = s.DivisionId,
            DivisionName = s.Division.Name,
            CreatedAt = s.CreatedAt,
            CreatedBy = s.CreatedBy
        }).ToList();
    }

    public async Task<AdminScopeDto> AssignDivisionScopeAsync(
        AssignAdminScopeDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.RequireSystemAdministratorAsync(_context, actorUserId, mustBeActive: true, cancellationToken);

        var targetUser = await _userRepository.GetByIdAsync(dto.AdministratorUserId, includeNavigations: false, cancellationToken);

        if (targetUser == null)
        {
            throw new NotFoundException(nameof(User), dto.AdministratorUserId);
        }

        var division = await _divisionRepository.GetByIdAsync(dto.DivisionId, cancellationToken);

        if (division == null)
        {
            throw new NotFoundException(nameof(Division), dto.DivisionId);
        }

        var existingScope = await _adminScopeRepository.GetByAdminUserIdAsync(dto.AdministratorUserId, cancellationToken);

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

        _adminScopeRepository.Add(newScope);

        // Atomically revoke active refresh tokens for the affected administrator
        await _refreshTokenRepository.RevokeAllActiveByUserIdAsync(
            dto.AdministratorUserId, "ADMIN_SCOPE_ASSIGNED", "ADMIN_SCOPE_ASSIGNED", cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        var scope = await _adminScopeRepository.GetByIdAsync((int)adminScopeId, cancellationToken);

        if (scope == null)
        {
            throw new NotFoundException(nameof(AdministratorScope), adminScopeId);
        }

        var affectedUserId = scope.AdministratorUserId;
        var oldValues = $"{{\"AdministratorUserId\":\"{affectedUserId}\",\"DivisionId\":{scope.DivisionId}}}";

        _adminScopeRepository.Remove(scope);

        // Atomically revoke active refresh tokens for the affected administrator
        await _refreshTokenRepository.RevokeAllActiveByUserIdAsync(
            affectedUserId, "ADMIN_SCOPE_REVOKED", "ADMIN_SCOPE_REVOKED", cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        var grants = await _adminSystemAccessRepository.GetAllAsync(cancellationToken);

        return grants.Select(a => new AdminSystemAccessDto
        {
            AdministratorSystemAccessId = a.AdministratorSystemAccessId,
            AdministratorUserId = a.AdministratorUserId,
            SystemId = a.SystemId,
            SystemName = a.System.SystemName,
            CreatedAt = a.CreatedAt,
            CreatedBy = a.CreatedBy
        }).ToList();
    }

    public async Task<AdminSystemAccessDto> GrantAdminSystemAccessAsync(
        GrantAdminSystemAccessDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.RequireSystemAdministratorAsync(_context, actorUserId, mustBeActive: true, cancellationToken);

        var targetUser = await _userRepository.GetByIdAsync(dto.AdministratorUserId, includeNavigations: false, cancellationToken);

        if (targetUser == null)
        {
            throw new NotFoundException(nameof(User), dto.AdministratorUserId);
        }

        var system = await _systemRepository.GetByIdAsync(dto.SystemId, cancellationToken);

        if (system == null)
        {
            throw new NotFoundException(nameof(Domain.Entities.System), dto.SystemId);
        }

        var existingGrant = await _adminSystemAccessRepository.GetByAdminAndSystemAsync(
            dto.AdministratorUserId, dto.SystemId, cancellationToken);

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

        _adminSystemAccessRepository.Add(newGrant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        var grant = await _adminSystemAccessRepository.GetByIdAsync((int)adminSystemAccessId, cancellationToken);

        if (grant == null)
        {
            throw new NotFoundException(nameof(AdministratorSystemAccess), adminSystemAccessId);
        }

        var oldValues = $"{{\"AdministratorUserId\":\"{grant.AdministratorUserId}\",\"SystemId\":\"{grant.SystemId}\"}}";

        _adminSystemAccessRepository.Remove(grant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
