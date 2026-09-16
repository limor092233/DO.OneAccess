using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Domain.Entities.System> Systems => Set<Domain.Entities.System>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<UserSystemAccess> UserSystemAccess => Set<UserSystemAccess>();
    public DbSet<AdministratorScope> AdministratorScopes => Set<AdministratorScope>();
    public DbSet<AdministratorSystemAccess> AdministratorSystemAccess => Set<AdministratorSystemAccess>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
