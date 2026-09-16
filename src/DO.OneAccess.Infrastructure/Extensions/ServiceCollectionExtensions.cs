using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.Common.Interfaces.Persistence;
using DO.OneAccess.Application.Services;
using DO.OneAccess.Infrastructure.Persistence;
using DO.OneAccess.Infrastructure.Persistence.Queries;
using DO.OneAccess.Infrastructure.Persistence.Repositories;
using DO.OneAccess.Infrastructure.Security;

namespace DO.OneAccess.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructurePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Configuration 'ConnectionStrings:DefaultConnection' is required and cannot be null, empty, or whitespace.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IDivisionRepository, DivisionRepository>();
        services.AddScoped<ISectionRepository, SectionRepository>();
        services.AddScoped<ISystemRepository, SystemRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        services.AddScoped<IUserSystemAccessRepository, UserSystemAccessRepository>();
        services.AddScoped<IAdminScopeRepository, AdminScopeRepository>();
        services.AddScoped<IAdminSystemAccessRepository, AdminSystemAccessRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // Queries
        services.AddScoped<IDivisionQueries, DivisionQueries>();

        return services;
    }

    public static IServiceCollection AddInfrastructureAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var passwordIterations = configuration.GetValue<int?>("Security:PasswordIterations") ?? 100000;
        services.Configure<PasswordHasherOptions>(options =>
        {
            options.IterationCount = passwordIterations;
        });

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IBootstrapTokenService, BootstrapTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ILoginHistoryService, LoginHistoryService>();
        services.AddScoped<IAdminScopeService, AdminScopeService>();
        services.AddScoped<ISystemAccessService, SystemAccessService>();
        services.AddScoped<IDivisionService, DivisionService>();
        services.AddScoped<ISectionService, SectionService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISystemService, SystemService>();
        services.AddScoped<IFirstRunSetupService, FirstRunSetupService>();

        return services;
    }
}
