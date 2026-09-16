using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.Common.Security;
using DO.OneAccess.Application.DTOs;
using DO.OneAccess.Application.DTOs.Audit;
using DO.OneAccess.Application.DTOs.Users;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Domain.Enums;

namespace DO.OneAccess.Application.Services;

public class UserService : IUserService
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(
        IApplicationDbContext context,
        IAuditService auditService,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _auditService = auditService;
        _passwordHasher = passwordHasher;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(
        UserQueryDto query,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var adminDivisionId = await AuthorizationHelper.RequireAdminOrAboveScopeAsync(
            _context, actorUserId, mustBeActive: false, cancellationToken);

        var dbQuery = _context.Users
            .Include(u => u.Role)
            .Include(u => u.Section)
                .ThenInclude(s => s!.Division)
            .AsNoTracking();

        if (adminDivisionId.HasValue)
        {
            dbQuery = dbQuery.Where(u => u.Section != null && u.Section.DivisionId == adminDivisionId.Value);
        }

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(u => u.IsActive == query.IsActive.Value);
        }

        if (query.SectionId.HasValue)
        {
            dbQuery = dbQuery.Where(u => u.SectionId == query.SectionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.UsernameContains))
        {
            dbQuery = dbQuery.Where(u => u.Username.Contains(query.UsernameContains.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            dbQuery = dbQuery.Where(u =>
                u.Username.Contains(term) ||
                u.EmployeeNumber.Contains(term) ||
                u.FirstName.Contains(term) ||
                u.LastName.Contains(term) ||
                u.Email.Contains(term));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

        var items = await dbQuery
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                EmployeeNumber = u.EmployeeNumber,
                FirstName = u.FirstName,
                MiddleName = u.MiddleName,
                LastName = u.LastName,
                Email = u.Email,
                Position = u.Position,
                SectionId = u.SectionId,
                SectionName = u.Section != null ? u.Section.Name : null,
                DivisionId = u.Section != null ? u.Section.DivisionId : (int?)null,
                DivisionName = u.Section != null ? u.Section.Division.Name : null,
                RoleId = u.RoleId,
                RoleCode = u.Role.Code,
                Username = u.Username,
                IsActive = u.IsActive,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDto> GetUserByIdAsync(
        Guid userId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.ValidateUserScopeAsync(
            _context, actorUserId, userId, mustBeActive: false, cancellationToken);

        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Section)
                .ThenInclude(s => s!.Division)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException(nameof(User), userId);
        }

        return new UserDto
        {
            UserId = user.UserId,
            EmployeeNumber = user.EmployeeNumber,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            LastName = user.LastName,
            Email = user.Email,
            Position = user.Position,
            SectionId = user.SectionId,
            SectionName = user.Section != null ? user.Section.Name : null,
            DivisionId = user.Section != null ? user.Section.DivisionId : (int?)null,
            DivisionName = user.Section != null ? user.Section.Division.Name : null,
            RoleId = user.RoleId,
            RoleCode = user.Role.Code,
            Username = user.Username,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserDto> CreateUserAsync(
        CreateUserDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var actor = await AuthorizationHelper.GetAndValidateActorAsync(
            _context, actorUserId, mustBeActive: true, cancellationToken);

        // Guard: System Administrator accounts cannot be created
        if (dto.RoleId == (short)RoleType.SystemAdministrator)
        {
            throw new ForbiddenException("System Administrator accounts cannot be created. The System Administrator role may only be transferred.");
        }

        // Privilege escalation guard: Administrator can only create USER accounts
        if (actor.RoleId == (short)RoleType.Administrator && dto.RoleId != (short)RoleType.User)
        {
            throw new ForbiddenException("Administrators can only create user accounts with the User role.");
        }

        ValidateProfileFields(dto.EmployeeNumber, dto.FirstName, dto.MiddleName, dto.LastName, dto.Email, dto.Position, dto.Username, dto.Password);

        // Role-specific Section and Division validation
        string? resolvedDivisionName = null;
        if (dto.RoleId == (short)RoleType.User)
        {
            if (!dto.SectionId.HasValue || dto.SectionId.Value <= 0)
            {
                throw new ValidationException(nameof(dto.SectionId), "Section is required for User accounts.");
            }

            var section = await _context.Sections
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SectionId == dto.SectionId.Value, cancellationToken);

            if (section == null)
            {
                throw new NotFoundException(nameof(Section), dto.SectionId.Value);
            }

            var division = await _context.Divisions
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DivisionId == section.DivisionId, cancellationToken);

            if (division == null)
            {
                throw new NotFoundException(nameof(Division), section.DivisionId);
            }

            if (!section.IsActive)
            {
                throw new ValidationException(nameof(dto.SectionId), "Selected section is inactive.");
            }

            if (!division.IsActive)
            {
                throw new ValidationException(nameof(dto.SectionId), "Selected section's parent division is inactive.");
            }

            if (dto.DivisionId.HasValue && section.DivisionId != dto.DivisionId.Value)
            {
                throw new ValidationException(nameof(dto.SectionId), "Selected section does not belong to the selected division.");
            }

            await AuthorizationHelper.ValidateSectionScopeAsync(
                _context, actorUserId, dto.SectionId.Value, mustBeActive: true, cancellationToken);
        }
        else if (dto.RoleId == (short)RoleType.Administrator)
        {
            if (!dto.DivisionId.HasValue || dto.DivisionId.Value <= 0)
            {
                throw new ValidationException(nameof(dto.DivisionId), "Division is required for Administrator accounts.");
            }

            var division = await _context.Divisions
                .FirstOrDefaultAsync(d => d.DivisionId == dto.DivisionId.Value, cancellationToken);

            if (division == null)
            {
                throw new NotFoundException(nameof(Division), dto.DivisionId.Value);
            }

            if (!division.IsActive)
            {
                throw new ValidationException(nameof(dto.DivisionId), "Selected division is inactive.");
            }

            resolvedDivisionName = division.Name;
        }

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleId == dto.RoleId, cancellationToken);

        if (role == null)
        {
            throw new NotFoundException(nameof(Role), dto.RoleId);
        }

        var usernameExists = await _context.Users
            .AnyAsync(u => u.Username == dto.Username.Trim(), cancellationToken);

        if (usernameExists)
        {
            throw new ConflictException($"A user with username '{dto.Username.Trim()}' already exists.");
        }

        var employeeNumberExists = await _context.Users
            .AnyAsync(u => u.EmployeeNumber == dto.EmployeeNumber.Trim(), cancellationToken);

        if (employeeNumberExists)
        {
            throw new ConflictException($"A user with employee number '{dto.EmployeeNumber.Trim()}' already exists.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            UserId = Guid.NewGuid(),
            EmployeeNumber = dto.EmployeeNumber.Trim(),
            FirstName = dto.FirstName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Position = string.IsNullOrWhiteSpace(dto.Position) ? null : dto.Position.Trim(),
            SectionId = dto.RoleId == (short)RoleType.User ? dto.SectionId : null,
            RoleId = dto.RoleId,
            Username = dto.Username.Trim(),
            IsActive = true,
            CreatedAt = now,
            CreatedBy = actorUserId
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        _context.Users.Add(user);

        if (dto.RoleId == (short)RoleType.Administrator)
        {
            var adminScope = new AdministratorScope
            {
                AdministratorUserId = user.UserId,
                DivisionId = dto.DivisionId!.Value,
                CreatedAt = now,
                CreatedBy = actorUserId
            };
            _context.AdministratorScopes.Add(adminScope);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "CREATE_USER",
            EntityName = nameof(User),
            EntityId = user.UserId.ToString(),
            NewValues = $"{{\"Username\":\"{user.Username}\",\"RoleId\":{user.RoleId},\"EmployeeNumber\":\"{user.EmployeeNumber}\",\"Email\":\"{user.Email}\"}}"
        }, cancellationToken);

        // Load section details for DTO response if assigned
        string? sectionName = null;
        int? divisionId = null;
        string? divisionName = null;
        if (user.SectionId.HasValue)
        {
            var sec = await _context.Sections
                .Include(s => s.Division)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SectionId == user.SectionId.Value, cancellationToken);
            if (sec != null)
            {
                sectionName = sec.Name;
                divisionId = sec.DivisionId;
                divisionName = sec.Division.Name;
            }
        }
        else if (dto.RoleId == (short)RoleType.Administrator)
        {
            divisionId = dto.DivisionId;
            divisionName = resolvedDivisionName;
        }

        return new UserDto
        {
            UserId = user.UserId,
            EmployeeNumber = user.EmployeeNumber,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            LastName = user.LastName,
            Email = user.Email,
            Position = user.Position,
            SectionId = user.SectionId,
            SectionName = sectionName,
            DivisionId = divisionId,
            DivisionName = divisionName,
            RoleId = user.RoleId,
            RoleCode = role.Code,
            Username = user.Username,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task TransferSystemAdministratorAsync(
        TransferSystemAdministratorDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Actor must be active System Administrator
        var actor = await AuthorizationHelper.RequireSystemAdministratorAsync(
            _context, actorUserId, mustBeActive: true, cancellationToken);

        // 2. Target validation
        if (dto.TargetUserId == Guid.Empty)
        {
            throw new ValidationException(nameof(dto.TargetUserId), "Target user ID is required.");
        }

        if (dto.TargetUserId == actorUserId)
        {
            throw new ValidationException(nameof(dto.TargetUserId), "Self-transfer is not permitted.");
        }

        // 3. Validate previous admin's new role
        if (dto.PreviousAdminNewRoleId != (short)RoleType.Administrator &&
            dto.PreviousAdminNewRoleId != (short)RoleType.User)
        {
            throw new ValidationException(nameof(dto.PreviousAdminNewRoleId), "Previous System Administrator must transition to either Administrator or User role.");
        }

        if (dto.PreviousAdminNewRoleId == (short)RoleType.Administrator)
        {
            if (!dto.PreviousAdminDivisionId.HasValue || dto.PreviousAdminDivisionId.Value <= 0)
            {
                throw new ValidationException(nameof(dto.PreviousAdminDivisionId), "Division is required when transitioning to Administrator role.");
            }
        }
        else if (dto.PreviousAdminNewRoleId == (short)RoleType.User)
        {
            if (!dto.PreviousAdminSectionId.HasValue || dto.PreviousAdminSectionId.Value <= 0)
            {
                throw new ValidationException(nameof(dto.PreviousAdminSectionId), "Section is required when transitioning to User role.");
            }
        }

        // Serializable transaction boundary
        using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        try
        {
            var targetUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == dto.TargetUserId, cancellationToken);

            if (targetUser == null)
            {
                throw new NotFoundException(nameof(User), dto.TargetUserId);
            }

            if (!targetUser.IsActive)
            {
                throw new ValidationException(nameof(dto.TargetUserId), "Target user is inactive. System Administrator role can only be transferred to an active account.");
            }

            if (targetUser.RoleId == (short)RoleType.SystemAdministrator)
            {
                throw new ConflictException("Target user is already a System Administrator.");
            }

            // Validate previous admin new assignment entities
            if (dto.PreviousAdminNewRoleId == (short)RoleType.Administrator)
            {
                var div = await _context.Divisions
                    .FirstOrDefaultAsync(d => d.DivisionId == dto.PreviousAdminDivisionId!.Value, cancellationToken);
                if (div == null)
                {
                    throw new NotFoundException(nameof(Division), dto.PreviousAdminDivisionId!.Value);
                }
                if (!div.IsActive)
                {
                    throw new ValidationException(nameof(dto.PreviousAdminDivisionId), "Assigned division is inactive.");
                }
            }
            else if (dto.PreviousAdminNewRoleId == (short)RoleType.User)
            {
                var sec = await _context.Sections
                    .Include(s => s.Division)
                    .FirstOrDefaultAsync(s => s.SectionId == dto.PreviousAdminSectionId!.Value, cancellationToken);
                if (sec == null)
                {
                    throw new NotFoundException(nameof(Section), dto.PreviousAdminSectionId!.Value);
                }
                if (!sec.IsActive)
                {
                    throw new ValidationException(nameof(dto.PreviousAdminSectionId), "Assigned section is inactive.");
                }
            }

            var now = DateTime.UtcNow;

            // Clean up target Administrator-specific access grants (intentional role transition cleanup)
            var targetScopes = await _context.AdministratorScopes
                .Where(s => s.AdministratorUserId == targetUser.UserId)
                .ToListAsync(cancellationToken);
            if (targetScopes.Count > 0)
            {
                _context.AdministratorScopes.RemoveRange(targetScopes);
            }

            var targetSystemAccesses = await _context.AdministratorSystemAccess
                .Where(a => a.AdministratorUserId == targetUser.UserId)
                .ToListAsync(cancellationToken);
            if (targetSystemAccesses.Count > 0)
            {
                _context.AdministratorSystemAccess.RemoveRange(targetSystemAccesses);
            }

            // Transition target to System Administrator
            var targetPreviousRoleId = targetUser.RoleId;
            targetUser.RoleId = (short)RoleType.SystemAdministrator;
            targetUser.SectionId = null;
            targetUser.UpdatedAt = now;
            targetUser.UpdatedBy = actorUserId;

            // Transition actor (previous System Administrator) to new role
            actor.RoleId = dto.PreviousAdminNewRoleId;
            actor.UpdatedAt = now;
            actor.UpdatedBy = actorUserId;

            if (dto.PreviousAdminNewRoleId == (short)RoleType.Administrator)
            {
                actor.SectionId = null;
                // Add AdministratorScope for previous admin
                var newScope = new AdministratorScope
                {
                    AdministratorUserId = actor.UserId,
                    DivisionId = dto.PreviousAdminDivisionId!.Value,
                    CreatedAt = now,
                    CreatedBy = actorUserId
                };
                _context.AdministratorScopes.Add(newScope);
            }
            else if (dto.PreviousAdminNewRoleId == (short)RoleType.User)
            {
                actor.SectionId = dto.PreviousAdminSectionId!.Value;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Invariant verification: exactly one System Administrator must exist
            var sysAdminCount = await _context.Users
                .CountAsync(u => u.RoleId == (short)RoleType.SystemAdministrator, cancellationToken);

            if (sysAdminCount != 1)
            {
                throw new InvalidOperationException("System Administrator invariant violation: exactly one System Administrator is permitted.");
            }

            // Revoke active refresh tokens for both users to ensure updated claims on next refresh
            var refreshTokensToRevoke = await _context.RefreshTokens
                .Where(rt => (rt.UserId == actor.UserId || rt.UserId == targetUser.UserId) && rt.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var rt in refreshTokensToRevoke)
            {
                rt.RevokedAt = now;
                rt.RevokedByIp = "SYSTEM_ROLE_TRANSFER";
            }
            if (refreshTokensToRevoke.Count > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Audit log emission (no credentials, tokens, or passwords logged)
            await _auditService.LogAsync(new WriteAuditLogDto
            {
                UserId = actorUserId,
                Action = "TRANSFER_SYSTEM_ADMINISTRATOR",
                EntityName = nameof(User),
                EntityId = targetUser.UserId.ToString(),
                OldValues = $"{{\"PreviousSysAdminUserId\":\"{actor.UserId}\",\"PreviousSysAdminUsername\":\"{actor.Username}\",\"TargetUserId\":\"{targetUser.UserId}\",\"TargetPreviousRoleId\":{targetPreviousRoleId}}}",
                NewValues = $"{{\"NewSysAdminUserId\":\"{targetUser.UserId}\",\"NewSysAdminUsername\":\"{targetUser.Username}\",\"PreviousSysAdminNewRoleId\":{actor.RoleId},\"PreviousSysAdminSectionId\":{(actor.SectionId.HasValue ? actor.SectionId.Value.ToString() : "null")},\"PreviousSysAdminDivisionId\":{(dto.PreviousAdminDivisionId.HasValue ? dto.PreviousAdminDivisionId.Value.ToString() : "null")}}}"
            }, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<UserDto> UpdateUserAsync(
        Guid userId,
        UpdateUserDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizationHelper.ValidateUserScopeAsync(
            _context, actorUserId, userId, mustBeActive: true, cancellationToken);

        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Section)
                .ThenInclude(s => s!.Division)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException(nameof(User), userId);
        }

        var oldValues = $"{{\"Username\":\"{user.Username}\",\"IsActive\":{user.IsActive.ToString().ToLowerInvariant()},\"FirstName\":\"{user.FirstName}\",\"LastName\":\"{user.LastName}\"}}";

        if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username.Trim() != user.Username)
        {
            var trimmedUsername = dto.Username.Trim();
            if (trimmedUsername.Length > 50)
            {
                throw new ValidationException(nameof(dto.Username), "Username must not exceed 50 characters.");
            }

            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == trimmedUsername && u.UserId != userId, cancellationToken);

            if (usernameExists)
            {
                throw new ConflictException($"A user with username '{trimmedUsername}' already exists.");
            }

            user.Username = trimmedUsername;
        }

        if (dto.FirstName != null)
        {
            if (string.IsNullOrWhiteSpace(dto.FirstName))
            {
                throw new ValidationException(nameof(dto.FirstName), "First Name is required.");
            }
            if (dto.FirstName.Trim().Length > 100)
            {
                throw new ValidationException(nameof(dto.FirstName), "First Name must not exceed 100 characters.");
            }
            user.FirstName = dto.FirstName.Trim();
        }

        if (dto.MiddleName != null)
        {
            if (dto.MiddleName.Trim().Length > 100)
            {
                throw new ValidationException(nameof(dto.MiddleName), "Middle Name must not exceed 100 characters.");
            }
            user.MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim();
        }

        if (dto.LastName != null)
        {
            if (string.IsNullOrWhiteSpace(dto.LastName))
            {
                throw new ValidationException(nameof(dto.LastName), "Last Name is required.");
            }
            if (dto.LastName.Trim().Length > 100)
            {
                throw new ValidationException(nameof(dto.LastName), "Last Name must not exceed 100 characters.");
            }
            user.LastName = dto.LastName.Trim();
        }

        if (dto.Email != null)
        {
            var trimmedEmail = dto.Email.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(trimmedEmail))
            {
                throw new ValidationException(nameof(dto.Email), "Email is required.");
            }
            if (trimmedEmail.Length > 200)
            {
                throw new ValidationException(nameof(dto.Email), "Email must not exceed 200 characters.");
            }
            if (!EmailRegex.IsMatch(trimmedEmail))
            {
                throw new ValidationException(nameof(dto.Email), "Email is not in a valid format.");
            }
            user.Email = trimmedEmail;
        }

        if (dto.Position != null)
        {
            if (dto.Position.Trim().Length > 100)
            {
                throw new ValidationException(nameof(dto.Position), "Position must not exceed 100 characters.");
            }
            user.Position = string.IsNullOrWhiteSpace(dto.Position) ? null : dto.Position.Trim();
        }

        if (dto.SectionId.HasValue)
        {
            if (user.RoleId == (short)RoleType.User)
            {
                if (dto.SectionId.Value <= 0)
                {
                    throw new ValidationException(nameof(dto.SectionId), "Section cannot be cleared for standard User accounts.");
                }

                // 1. Load the Section
                var section = await _context.Sections
                    .FirstOrDefaultAsync(s => s.SectionId == dto.SectionId.Value, cancellationToken);

                // 3. Validate Section existence
                if (section == null)
                {
                    throw new NotFoundException(nameof(Section), dto.SectionId.Value);
                }

                // 2. Explicitly load or query its parent Division
                var division = await _context.Divisions
                    .FirstOrDefaultAsync(d => d.DivisionId == section.DivisionId, cancellationToken);

                if (division == null)
                {
                    throw new NotFoundException(nameof(Division), section.DivisionId);
                }

                // 4. Validate Section.IsActive
                if (!section.IsActive)
                {
                    throw new ValidationException(nameof(dto.SectionId), "Selected section is inactive.");
                }

                // 5. Validate parent Division.IsActive
                if (!division.IsActive)
                {
                    throw new ValidationException(nameof(dto.SectionId), "Selected section's parent division is inactive.");
                }

                // 6. Validate the actor's Administrator Division Scope
                await AuthorizationHelper.ValidateDivisionScopeAsync(
                    _context, actorUserId, section.DivisionId, mustBeActive: true, cancellationToken);

                // 7. Only then allow the assignment
                user.SectionId = dto.SectionId.Value;
            }
            else if (user.RoleId == (short)RoleType.Administrator)
            {
                if (dto.SectionId.Value > 0)
                {
                    var section = await _context.Sections
                        .FirstOrDefaultAsync(s => s.SectionId == dto.SectionId.Value, cancellationToken);

                    if (section == null)
                    {
                        throw new NotFoundException(nameof(Section), dto.SectionId.Value);
                    }

                    var division = await _context.Divisions
                        .FirstOrDefaultAsync(d => d.DivisionId == section.DivisionId, cancellationToken);

                    if (division == null)
                    {
                        throw new NotFoundException(nameof(Division), section.DivisionId);
                    }

                    if (!section.IsActive)
                    {
                        throw new ValidationException(nameof(dto.SectionId), "Selected section is inactive.");
                    }

                    if (!division.IsActive)
                    {
                        throw new ValidationException(nameof(dto.SectionId), "Selected section's parent division is inactive.");
                    }

                    user.SectionId = dto.SectionId.Value;
                }
                else
                {
                    user.SectionId = null;
                }
            }
            else if (user.RoleId == (short)RoleType.SystemAdministrator)
            {
                if (dto.SectionId.Value > 0)
                {
                    throw new ValidationException(nameof(dto.SectionId), "System Administrator accounts cannot be assigned to a section.");
                }

                user.SectionId = null;
            }
        }

        if (dto.IsActive.HasValue)
        {
            if (!dto.IsActive.Value && userId == actorUserId)
            {
                throw new ForbiddenException("Self-deactivation is not permitted.");
            }

            user.IsActive = dto.IsActive.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = actorUserId;

        await _context.SaveChangesAsync(cancellationToken);

        var newValues = $"{{\"Username\":\"{user.Username}\",\"IsActive\":{user.IsActive.ToString().ToLowerInvariant()},\"FirstName\":\"{user.FirstName}\",\"LastName\":\"{user.LastName}\"}}";

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "UPDATE_USER",
            EntityName = nameof(User),
            EntityId = user.UserId.ToString(),
            OldValues = oldValues,
            NewValues = newValues
        }, cancellationToken);

        // Reload Section navigation to reflect any update
        string? sectionName = null;
        int? divisionId = null;
        string? divisionName = null;
        if (user.SectionId.HasValue)
        {
            var sec = await _context.Sections
                .Include(s => s.Division)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SectionId == user.SectionId.Value, cancellationToken);
            if (sec != null)
            {
                sectionName = sec.Name;
                divisionId = sec.DivisionId;
                divisionName = sec.Division.Name;
            }
        }

        return new UserDto
        {
            UserId = user.UserId,
            EmployeeNumber = user.EmployeeNumber,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            LastName = user.LastName,
            Email = user.Email,
            Position = user.Position,
            SectionId = user.SectionId,
            SectionName = sectionName,
            DivisionId = divisionId,
            DivisionName = divisionName,
            RoleId = user.RoleId,
            RoleCode = user.Role.Code,
            Username = user.Username,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task DeactivateUserAsync(
        Guid userId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (userId == actorUserId)
        {
            throw new ForbiddenException("Self-deactivation is not permitted.");
        }

        await AuthorizationHelper.ValidateUserScopeAsync(
            _context, actorUserId, userId, mustBeActive: true, cancellationToken);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException(nameof(User), userId);
        }

        var oldValues = $"{{\"IsActive\":{user.IsActive.ToString().ToLowerInvariant()}}}";

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = actorUserId;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "DEACTIVATE_USER",
            EntityName = nameof(User),
            EntityId = user.UserId.ToString(),
            OldValues = oldValues,
            NewValues = "{\"IsActive\":false}"
        }, cancellationToken);
    }

    public async Task ChangeRoleAsync(
        Guid userId,
        short roleId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        // Rule: Administrator cannot perform role changes. Only System Administrator may change roles.
        await AuthorizationHelper.RequireSystemAdministratorAsync(
            _context, actorUserId, mustBeActive: true, cancellationToken);

        // Rule: Self-role modification is not permitted.
        if (userId == actorUserId)
        {
            throw new ForbiddenException("Self-role modification is not permitted.");
        }

        if (roleId == (short)RoleType.SystemAdministrator)
        {
            throw new ForbiddenException("System Administrator role cannot be assigned through standard role modification. The System Administrator role may only be transferred.");
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException(nameof(User), userId);
        }

        var targetRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleId == roleId, cancellationToken);

        if (targetRole == null)
        {
            throw new NotFoundException(nameof(Role), roleId);
        }

        var oldRole = user.Role.Code;
        var oldValues = $"{{\"RoleId\":{user.RoleId},\"RoleCode\":\"{oldRole}\"}}";

        user.RoleId = roleId;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = actorUserId;

        await _context.SaveChangesAsync(cancellationToken);

        var newValues = $"{{\"RoleId\":{targetRole.RoleId},\"RoleCode\":\"{targetRole.Code}\"}}";

        await _auditService.LogAsync(new WriteAuditLogDto
        {
            UserId = actorUserId,
            Action = "CHANGE_USER_ROLE",
            EntityName = nameof(User),
            EntityId = user.UserId.ToString(),
            OldValues = oldValues,
            NewValues = newValues
        }, cancellationToken);
    }

    private static void ValidateProfileFields(
        string employeeNumber,
        string firstName,
        string? middleName,
        string lastName,
        string email,
        string? position,
        string username,
        string password)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
            throw new ValidationException(nameof(employeeNumber), "Employee Number is required.");
        if (employeeNumber.Trim().Length > 50)
            throw new ValidationException(nameof(employeeNumber), "Employee Number must not exceed 50 characters.");

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ValidationException(nameof(firstName), "First Name is required.");
        if (firstName.Trim().Length > 100)
            throw new ValidationException(nameof(firstName), "First Name must not exceed 100 characters.");

        if (!string.IsNullOrWhiteSpace(middleName) && middleName.Trim().Length > 100)
            throw new ValidationException(nameof(middleName), "Middle Name must not exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ValidationException(nameof(lastName), "Last Name is required.");
        if (lastName.Trim().Length > 100)
            throw new ValidationException(nameof(lastName), "Last Name must not exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException(nameof(email), "Email is required.");
        if (email.Trim().Length > 200)
            throw new ValidationException(nameof(email), "Email must not exceed 200 characters.");
        if (!EmailRegex.IsMatch(email.Trim()))
            throw new ValidationException(nameof(email), "Email is not in a valid format.");

        if (!string.IsNullOrWhiteSpace(position) && position.Trim().Length > 100)
            throw new ValidationException(nameof(position), "Position must not exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(username))
            throw new ValidationException(nameof(username), "Username is required.");
        if (username.Trim().Length > 50)
            throw new ValidationException(nameof(username), "Username must not exceed 50 characters.");

        if (string.IsNullOrWhiteSpace(password))
            throw new ValidationException(nameof(password), "Password is required.");
    }
}
