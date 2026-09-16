using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.Services;
using DO.OneAccess.Infrastructure.Persistence;
using DO.OneAccess.Infrastructure.Security;

namespace DO.OneAccess.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructurePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost\\SQLEXPRESS;Database=DO_OneAccess;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

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
