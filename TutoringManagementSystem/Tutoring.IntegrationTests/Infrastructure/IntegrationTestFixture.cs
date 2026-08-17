using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;
using Tutoring.Infrastructure.Persistence;
namespace Tutoring.IntegrationTests.Infrastructure;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer =
        new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private Respawner _respawner = null!;

    public HttpClient Client { get; private set; } = null!;

    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        //  sql server container start
        await _sqlContainer.StartAsync();

        // 2. Uruchamiamy nasze API
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTests");

                builder.ConfigureServices(services =>
                {
                    // delete existing DbContextOptionsConfiguration<TutoringDbContext> registration
                    services.RemoveAll<
                        IDbContextOptionsConfiguration<TutoringDbContext>>();

                    // replace it with SQL Server from  Testcontainers
                    services.AddDbContext<TutoringDbContext>(options =>
                    {
                        options.UseSqlServer(
                            _sqlContainer.GetConnectionString());
                    });
                });
            });

        // Database migration
        using (var scope = Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<TutoringDbContext>();

            await dbContext.Database.MigrateAsync();
        }

        // database respawner
        await using var connection =
            new SqlConnection(_sqlContainer.GetConnectionString());

        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,

                TablesToIgnore =
                [
                    new Table("__EFMigrationsHistory")
                ]
            });

        // Create HttpClient for integration tests
        Client = _factory.CreateClient();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection =
            new SqlConnection(_sqlContainer.GetConnectionString());

        await connection.OpenAsync();

        await _respawner.ResetAsync(connection);
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();

        await _factory.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }
}