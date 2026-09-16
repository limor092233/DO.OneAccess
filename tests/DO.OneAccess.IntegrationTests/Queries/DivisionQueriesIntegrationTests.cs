namespace DO.OneAccess.IntegrationTests.Queries;

using System.Data.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using DO.OneAccess.Application.DTOs.Divisions;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Infrastructure.Persistence;
using DO.OneAccess.Infrastructure.Persistence.Queries;
using Xunit;

public class SqlCaptureInterceptor : DbCommandInterceptor
{
    public List<string> ExecutedCommands { get; } = new();

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        ExecutedCommands.Add(command.CommandText);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        ExecutedCommands.Add(command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}

[Collection("IntegrationTests")]
public class DivisionQueriesIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestDatabaseName = "DO_OneAccess_Test";
    private const string ConnectionString = $"Server=localhost\\SQLEXPRESS;Database={TestDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly WebApplicationFactory<Program> _factory;

    public DivisionQueriesIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString);
        if (!string.Equals(builder.InitialCatalog, TestDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Security Guard Violation: Targeted '{builder.InitialCatalog}'.");
        }

        _factory = factory.WithWebHostBuilder(hostBuilder =>
        {
            hostBuilder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
                if (contextDescriptor != null) services.Remove(contextDescriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(ConnectionString));
            });
        });

        InitializeTestDatabase();
    }

    private void InitializeTestDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var actualConnection = context.Database.GetConnectionString() ?? string.Empty;
        var actualBuilder = new SqlConnectionStringBuilder(actualConnection);
        if (!string.Equals(actualBuilder.InitialCatalog, TestDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ISOLATION VIOLATION in DivisionQueriesIntegrationTests");
        }

        context.Database.EnsureDeleted();
        context.Database.Migrate();
    }

    private (AppDbContext Context, SqlCaptureInterceptor Interceptor) CreateDbContextWithInterceptor()
    {
        var interceptor = new SqlCaptureInterceptor();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        return (new AppDbContext(options), interceptor);
    }

    [Fact]
    public async Task GetAllAsync_Unscoped_ReturnsAllDivisionsOrderedByName()
    {
        var (context, _) = CreateDbContextWithInterceptor();
        await using (context)
        {
            // Seed divisions out of alphabetical order
            context.Divisions.AddRange(
                new Division { Code = "Z_DIV", Name = "Zeta Division", Description = "Zeta", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Division { Code = "A_DIV", Name = "Alpha Division", Description = "Alpha", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Division { Code = "B_DIV", Name = "Beta Division", Description = "Beta", IsActive = false, CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var queries = new DivisionQueries(context);
            var results = await queries.GetAllAsync();

            Assert.NotNull(results);
            Assert.True(results.Count >= 3);

            var filtered = results.Where(r => r.Code is "Z_DIV" or "A_DIV" or "B_DIV").ToList();
            Assert.Equal(3, filtered.Count);
            Assert.Equal("Alpha Division", filtered[0].Name);
            Assert.Equal("Beta Division", filtered[1].Name);
            Assert.Equal("Zeta Division", filtered[2].Name);
            Assert.False(filtered[1].IsActive);
        }
    }

    [Fact]
    public async Task GetAllAsync_Scoped_ReturnsOnlyMatchingDivision()
    {
        var (context, _) = CreateDbContextWithInterceptor();
        await using (context)
        {
            var d1 = new Division { Code = "SCOPE_D1", Name = "Scoped Division 1", IsActive = true, CreatedAt = DateTime.UtcNow };
            var d2 = new Division { Code = "SCOPE_D2", Name = "Scoped Division 2", IsActive = true, CreatedAt = DateTime.UtcNow };
            context.Divisions.AddRange(d1, d2);
            await context.SaveChangesAsync();

            var queries = new DivisionQueries(context);
            var results = await queries.GetAllAsync(scopedDivisionId: d1.DivisionId);

            Assert.NotNull(results);
            var item = Assert.Single(results);
            Assert.Equal(d1.DivisionId, item.DivisionId);
            Assert.Equal("SCOPE_D1", item.Code);
            Assert.Equal("Scoped Division 1", item.Name);
        }
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsProjectedDtoWithCorrectValues()
    {
        var (context, _) = CreateDbContextWithInterceptor();
        await using (context)
        {
            var createdTime = new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);
            var division = new Division
            {
                Code = "DET_DIV",
                Name = "Detail Division",
                Description = "Detail Description",
                IsActive = true,
                CreatedAt = createdTime,
                CreatedBy = Guid.NewGuid()
            };
            context.Divisions.Add(division);
            await context.SaveChangesAsync();

            var queries = new DivisionQueries(context);
            var dto = await queries.GetByIdAsync(division.DivisionId);

            Assert.NotNull(dto);
            Assert.Equal(division.DivisionId, dto.DivisionId);
            Assert.Equal("DET_DIV", dto.Code);
            Assert.Equal("Detail Division", dto.Name);
            Assert.Equal("Detail Description", dto.Description);
            Assert.True(dto.IsActive);
            Assert.Equal(createdTime, dto.CreatedAt);
        }
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        var (context, _) = CreateDbContextWithInterceptor();
        await using (context)
        {
            var queries = new DivisionQueries(context);
            var dto = await queries.GetByIdAsync(999999);

            Assert.Null(dto);
        }
    }

    [Fact]
    public async Task GetByIdAsync_WithNullableDescription_ReturnsNullDescription()
    {
        var (context, _) = CreateDbContextWithInterceptor();
        await using (context)
        {
            var division = new Division
            {
                Code = "NULL_DESC",
                Name = "No Description Division",
                Description = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Divisions.Add(division);
            await context.SaveChangesAsync();

            var queries = new DivisionQueries(context);
            var dto = await queries.GetByIdAsync(division.DivisionId);

            Assert.NotNull(dto);
            Assert.Equal("NULL_DESC", dto.Code);
            Assert.Null(dto.Description);
        }
    }

    [Fact]
    public async Task SqlProjection_SelectsOnlyRequiredColumns_AndExcludesAuditAndNavigations()
    {
        var (context, interceptor) = CreateDbContextWithInterceptor();
        await using (context)
        {
            var division = new Division
            {
                Code = "SQL_TEST",
                Name = "SQL Test Division",
                Description = "SQL Verification",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid(),
                UpdatedBy = Guid.NewGuid(),
                UpdatedAt = DateTime.UtcNow
            };
            context.Divisions.Add(division);
            await context.SaveChangesAsync();

            interceptor.ExecutedCommands.Clear();

            var queries = new DivisionQueries(context);
            var result = await queries.GetByIdAsync(division.DivisionId);
            Assert.NotNull(result);

            // Verify the SQL command generated by EF Core
            var command = interceptor.ExecutedCommands.LastOrDefault();
            Assert.NotNull(command);

            var sqlUpper = command.ToUpperInvariant();

            // 1. Must contain all 6 DTO fields in projection
            Assert.Contains("DIVISIONID", sqlUpper);
            Assert.Contains("CODE", sqlUpper);
            Assert.Contains("NAME", sqlUpper);
            Assert.Contains("DESCRIPTION", sqlUpper);
            Assert.Contains("ISACTIVE", sqlUpper);
            Assert.Contains("CREATEDAT", sqlUpper);

            // 2. Must NOT select internal audit columns
            Assert.DoesNotContain("CREATEDBY", sqlUpper);
            Assert.DoesNotContain("UPDATEDBY", sqlUpper);
            Assert.DoesNotContain("UPDATEDAT", sqlUpper);

            // 3. Must NOT contain navigation joins
            Assert.DoesNotContain("JOIN", sqlUpper);
            Assert.DoesNotContain("SECTIONS", sqlUpper);
            Assert.DoesNotContain("ADMINISTRATORSCOPES", sqlUpper);
        }
    }
}
