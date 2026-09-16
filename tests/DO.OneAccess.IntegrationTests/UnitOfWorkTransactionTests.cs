using System.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Infrastructure.Persistence;
using DO.OneAccess.Infrastructure.Persistence.Repositories;
using Xunit;

namespace DO.OneAccess.IntegrationTests;

[Collection("IntegrationTests")]
public class UnitOfWorkTransactionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestDatabaseName = "DO_OneAccess_Test";
    private const string ConnectionString = $"Server=localhost\\SQLEXPRESS;Database={TestDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly WebApplicationFactory<Program> _factory;

    public UnitOfWorkTransactionTests(WebApplicationFactory<Program> factory)
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
            throw new InvalidOperationException("ISOLATION VIOLATION in UnitOfWorkTransactionTests");
        }

        context.Database.EnsureDeleted();
        context.Database.Migrate();
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task UnitOfWork_SaveChangesAsync_PersistsEntities()
    {
        await using var context = CreateDbContext();
        var uow = new UnitOfWork(context);
        var divisionRepo = new DivisionRepository(context);

        var division = new Division
        {
            Code = "UOW_DIV1",
            Name = "UoW Division 1",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        divisionRepo.Add(division);
        var affected = await uow.SaveChangesAsync();

        Assert.True(affected > 0);
        Assert.True(division.DivisionId > 0);

        var loaded = await divisionRepo.GetByCodeAsync("UOW_DIV1");
        Assert.NotNull(loaded);
        Assert.Equal("UoW Division 1", loaded.Name);
    }

    [Fact]
    public async Task UnitOfWorkTransaction_CommitAsync_PersistsTransactionally()
    {
        await using var context = CreateDbContext();
        var uow = new UnitOfWork(context);
        var divisionRepo = new DivisionRepository(context);

        await using (var tx = await uow.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            var division = new Division
            {
                Code = "TX_COMMIT_DIV",
                Name = "Tx Commit Division",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            divisionRepo.Add(division);
            await uow.SaveChangesAsync();
            await tx.CommitAsync();
        }

        await using var verifyContext = CreateDbContext();
        var verifyRepo = new DivisionRepository(verifyContext);
        var found = await verifyRepo.GetByCodeAsync("TX_COMMIT_DIV");
        Assert.NotNull(found);
    }

    [Fact]
    public async Task UnitOfWorkTransaction_DisposeWithoutCommit_AutomaticallyRollsBack()
    {
        await using var context = CreateDbContext();
        var uow = new UnitOfWork(context);
        var divisionRepo = new DivisionRepository(context);

        await using (var tx = await uow.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            var division = new Division
            {
                Code = "TX_ROLLBACK_DIV",
                Name = "Tx Rollback Division",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            divisionRepo.Add(division);
            await uow.SaveChangesAsync();
            // Intentionally not calling tx.CommitAsync()
        }

        await using var verifyContext = CreateDbContext();
        var verifyRepo = new DivisionRepository(verifyContext);
        var found = await verifyRepo.GetByCodeAsync("TX_ROLLBACK_DIV");
        Assert.Null(found);
    }

    [Fact]
    public async Task UnitOfWorkTransaction_ExplicitRollbackAsync_RollsBackChanges()
    {
        await using var context = CreateDbContext();
        var uow = new UnitOfWork(context);
        var divisionRepo = new DivisionRepository(context);

        await using (var tx = await uow.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            var division = new Division
            {
                Code = "TX_EXPLICIT_RB",
                Name = "Explicit Rollback Division",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            divisionRepo.Add(division);
            await uow.SaveChangesAsync();
            await tx.RollbackAsync();
        }

        await using var verifyContext = CreateDbContext();
        var verifyRepo = new DivisionRepository(verifyContext);
        var found = await verifyRepo.GetByCodeAsync("TX_EXPLICIT_RB");
        Assert.Null(found);
    }

    [Fact]
    public async Task UnitOfWorkTransaction_LeavesZeroOpenTransactions()
    {
        await using var context = CreateDbContext();
        var uow = new UnitOfWork(context);

        var tx = await uow.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        Assert.NotNull(context.Database.CurrentTransaction);

        await tx.DisposeAsync();
        Assert.Null(context.Database.CurrentTransaction);
    }
}
