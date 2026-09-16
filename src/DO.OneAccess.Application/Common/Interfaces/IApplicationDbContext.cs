using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Role> Roles { get; }
    DbSet<Division> Divisions { get; }
    DbSet<Section> Sections { get; }
    DbSet<User> Users { get; }
    DbSet<Domain.Entities.System> Systems { get; }
    DbSet<SystemSetting> SystemSettings { get; }
    DbSet<UserSystemAccess> UserSystemAccess { get; }
    DbSet<AdministratorScope> AdministratorScopes { get; }
    DbSet<AdministratorSystemAccess> AdministratorSystemAccess { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<LoginHistory> LoginHistories { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
