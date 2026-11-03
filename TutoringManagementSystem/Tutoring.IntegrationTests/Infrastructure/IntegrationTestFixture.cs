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
using Tutoring.Infrastructure.FileStorage;
using System.Text.RegularExpressions;
namespace Tutoring.IntegrationTests.Infrastructure;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer =
        new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private Respawner _respawner = null!;
    private string _adminConnectionString = null!;

    public string MigrationConnectionString { get; private set; } = null!;

    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), "tutoring-upload-tests", Guid.NewGuid().ToString("N"));

    public HttpClient Client { get; private set; } = null!;

    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        //  sql server container start
        await _sqlContainer.StartAsync();

        var appPassword = $"App-{Guid.NewGuid():N}!";
        var migrationPassword = $"Migration-{Guid.NewGuid():N}!";
        var connectionBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
        await using (var admin = new SqlConnection(connectionBuilder.ConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new SqlCommand("CREATE DATABASE TutoringDb;", admin);
            await create.ExecuteNonQueryAsync();
        }

        connectionBuilder.InitialCatalog = "TutoringDb";
        _adminConnectionString = connectionBuilder.ConnectionString;
        var securitySql = (await File.ReadAllTextAsync(
                Path.Combine(AppContext.BaseDirectory, "TestAssets", "least-privilege.sql")))
            .Replace("$(APP_PASSWORD_SQL)", appPassword)
            .Replace("$(MIGRATIONS_PASSWORD_SQL)", migrationPassword);
        await using (var admin = new SqlConnection(_adminConnectionString))
        {
            await admin.OpenAsync();
            // Run the actual provisioning script twice to cover an existing installation.
            for (var run = 0; run < 2; run++)
                foreach (var batch in Regex.Split(securitySql, @"^GO\s*$", RegexOptions.Multiline))
                {
                    if (string.IsNullOrWhiteSpace(batch)) continue;
                    await using var command = new SqlCommand(batch, admin);
                    await command.ExecuteNonQueryAsync();
                }
        }

        connectionBuilder.UserID = "tutoring_migrations";
        connectionBuilder.Password = migrationPassword;
        MigrationConnectionString = connectionBuilder.ConnectionString;
        var migrationOptions = new DbContextOptionsBuilder<TutoringDbContext>()
            .UseSqlServer(MigrationConnectionString).Options;
        await using (var migrations = new TutoringDbContext(migrationOptions))
        {
            await migrations.Database.MigrateAsync();
            await migrations.Database.MigrateAsync();
        }

        connectionBuilder.UserID = "tutoring_app";
        connectionBuilder.Password = appPassword;
        var runtimeConnectionString = connectionBuilder.ConnectionString;

        // 2. Uruchamiamy nasze API
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTests");

                builder.ConfigureServices(services =>
                {
                    services.Configure<FileStorageOptions>(options => options.RootPath = StorageRoot);
                    // delete existing DbContextOptionsConfiguration<TutoringDbContext> registration
                    services.RemoveAll<
                        IDbContextOptionsConfiguration<TutoringDbContext>>();

                    // replace it with SQL Server from  Testcontainers
                    services.AddDbContext<TutoringDbContext>(options =>
                    {
                        options.UseSqlServer(
                            runtimeConnectionString);
                    });
                });
            });

        // database respawner
        await using var connection =
            new SqlConnection(_adminConnectionString);

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
            new SqlConnection(_adminConnectionString);

        await connection.OpenAsync();

        await _respawner.ResetAsync(connection);
        if (Directory.Exists(StorageRoot))
            foreach (var file in Directory.EnumerateFiles(StorageRoot))
                File.Delete(file);
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();

        await _factory.DisposeAsync();
        await _sqlContainer.DisposeAsync();
        if (Directory.Exists(StorageRoot))
            Directory.Delete(StorageRoot, recursive: true);
    }
}
