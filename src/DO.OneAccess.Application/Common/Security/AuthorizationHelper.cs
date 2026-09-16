using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Domain.Enums;

namespace DO.OneAccess.Application.Common.Security;

/// <summary>
/// Helper methods for fine-grained application-layer authorization and scope enforcement.
/// </summary>
public static class AuthorizationHelper
{
    /// <summary>
    /// Validates that the actor exists and is active (re-verifying from DB for state-mutating operations).
    /// Returns the actor User entity including Role.
    /// </summary>
    public static async Task<User> GetAndValidateActorAsync(
        IApplicationDbContext dbContext,
        Guid actorUserId,
        bool mustBeActive = true,
        CancellationToken cancellationToken = default)
    {
        var actor = await dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == actorUserId, cancellationToken);

        if (actor == null)
        {
            throw new ForbiddenException("Actor user was not found.");
        }

        if (mustBeActive && !actor.IsActive)
        {
            throw new ForbiddenException("Actor account is inactive.");
        }

        return actor;
    }

    /// <summary>
    /// Asserts that the actor is a System Administrator. Throws ForbiddenException otherwise.
    /// </summary>
    public static async Task<User> RequireSystemAdministratorAsync(
        IApplicationDbContext dbContext,
        Guid actorUserId,
        bool mustBeActive = true,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetAndValidateActorAsync(dbContext, actorUserId, mustBeActive, cancellationToken);

        if (actor.RoleId != (short)RoleType.SystemAdministrator)
        {
            throw new ForbiddenException("Operation requires System Administrator privileges.");
        }

        return actor;
    }

    /// <summary>
    /// Asserts that the actor is Administrator or System Administrator.
    /// If Administrator, returns their assigned DivisionId.
    /// If System Administrator, returns null (global scope).
    /// </summary>
    public static async Task<int?> RequireAdminOrAboveScopeAsync(
        IApplicationDbContext dbContext,
        Guid actorUserId,
        bool mustBeActive = true,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetAndValidateActorAsync(dbContext, actorUserId, mustBeActive, cancellationToken);

        if (actor.RoleId == (short)RoleType.SystemAdministrator)
        {
            return null; // Global scope
        }

        if (actor.RoleId == (short)RoleType.Administrator)
        {
            var adminScope = await dbContext.AdministratorScopes
                .FirstOrDefaultAsync(s => s.AdministratorUserId == actorUserId, cancellationToken);

            if (adminScope == null)
            {
                throw new ForbiddenException("Administrator scope not assigned.");
            }

            var division = await dbContext.Divisions
                .FirstOrDefaultAsync(d => d.DivisionId == adminScope.DivisionId, cancellationToken);

            if (division == null)
            {
                throw new ForbiddenException("Assigned division does not exist.");
            }

            if (!division.IsActive)
            {
                throw new ForbiddenException("Assigned division is inactive.");
            }

            return adminScope.DivisionId;
        }

        throw new ForbiddenException("Operation requires administrative privileges.");
    }

    /// <summary>
    /// Validates that the target division is within the actor's scope.
    /// </summary>
    public static async Task ValidateDivisionScopeAsync(
        IApplicationDbContext dbContext,
        Guid actorUserId,
        int targetDivisionId,
        bool mustBeActive = true,
        CancellationToken cancellationToken = default)
    {
        var scopedDivisionId = await RequireAdminOrAboveScopeAsync(dbContext, actorUserId, mustBeActive, cancellationToken);
        if (scopedDivisionId.HasValue && scopedDivisionId.Value != targetDivisionId)
        {
            throw new ForbiddenException("Operation is outside administrator's assigned division scope.");
        }
    }

    /// <summary>
    /// Resolves the division ID for a given section and validates against the actor's scope.
    /// Returns the DivisionId of the section.
    /// </summary>
    public static async Task<int> ValidateSectionScopeAsync(
        IApplicationDbContext dbContext,
        Guid actorUserId,
        int targetSectionId,
        bool mustBeActive = true,
        CancellationToken cancellationToken = default)
    {
        var section = await dbContext.Sections
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SectionId == targetSectionId, cancellationToken);

        if (section == null)
        {
            throw new NotFoundException(nameof(Section), targetSectionId);
        }

        await ValidateDivisionScopeAsync(dbContext, actorUserId, section.DivisionId, mustBeActive, cancellationToken);
        return section.DivisionId;
    }

    /// <summary>
    /// Resolves the division ID for a given user and validates against the actor's scope.
    /// System Administrator has global scope.
    /// Administrator can only manage standard User accounts in their assigned Division.
    /// </summary>
    public static async Task ValidateUserScopeAsync(
        IApplicationDbContext dbContext,
        Guid actorUserId,
        Guid targetUserId,
        bool mustBeActive = true,
        CancellationToken cancellationToken = default)
    {
        var scopedDivisionId = await RequireAdminOrAboveScopeAsync(dbContext, actorUserId, mustBeActive, cancellationToken);
        if (!scopedDivisionId.HasValue)
        {
            return; // System Administrator has global scope
        }

        var user = await dbContext.Users
            .Include(u => u.Section)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == targetUserId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException(nameof(User), targetUserId);
        }

        // Rule: Administrators cannot manage System Administrators or other Administrators
        if (user.RoleId != (short)RoleType.User)
        {
            throw new ForbiddenException("Administrators can only manage standard User accounts.");
        }

        if (user.Section == null)
        {
            throw new ForbiddenException("Administrator cannot manage users without an assigned section.");
        }

        await ValidateDivisionScopeAsync(dbContext, actorUserId, user.Section.DivisionId, mustBeActive, cancellationToken);
    }

    /// <summary>
    /// Validates that the actor has permission to manage the specified system.
    /// System Administrator has global access. Administrator requires an AdministratorSystemAccess record.
    /// </summary>
    public static async Task ValidateAdminSystemAccessAsync(
        IApplicationDbContext dbContext,
        Guid actorUserId,
        Guid systemId,
        bool mustBeActive = true,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetAndValidateActorAsync(dbContext, actorUserId, mustBeActive, cancellationToken);

        if (actor.RoleId == (short)RoleType.SystemAdministrator)
        {
            return; // Global access
        }

        if (actor.RoleId == (short)RoleType.Administrator)
        {
            var hasAccess = await dbContext.AdministratorSystemAccess
                .AnyAsync(a => a.AdministratorUserId == actorUserId && a.SystemId == systemId, cancellationToken);

            if (!hasAccess)
            {
                throw new ForbiddenException("Administrator is not authorized to manage this system.");
            }

            return;
        }

        throw new ForbiddenException("Operation requires administrative privileges.");
    }
}
