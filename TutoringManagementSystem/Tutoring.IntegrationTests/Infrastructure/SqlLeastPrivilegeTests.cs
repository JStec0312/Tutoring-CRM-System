using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.IntegrationTests.Infrastructure;

[Collection(IntegrationTestCollection.Name)]
public sealed class SqlLeastPrivilegeTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task RuntimeIdentity_ShouldBeApplicationLoginWithCrudInBothSchemas()
    {
        await using var connection = await OpenRuntimeConnectionAsync();
        await using var command = new SqlCommand("""
            SELECT ORIGINAL_LOGIN(), IS_SRVROLEMEMBER('sysadmin'), IS_MEMBER('db_owner'),
                HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'SELECT'),
                HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'INSERT'),
                HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'UPDATE'),
                HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'DELETE'),
                HAS_PERMS_BY_NAME('messaging', 'SCHEMA', 'SELECT'),
                HAS_PERMS_BY_NAME('messaging', 'SCHEMA', 'INSERT'),
                HAS_PERMS_BY_NAME('messaging', 'SCHEMA', 'UPDATE'),
                HAS_PERMS_BY_NAME('messaging', 'SCHEMA', 'DELETE');
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("tutoring_app", reader.GetString(0));
        Assert.Equal(0, reader.GetInt32(1));
        Assert.Equal(0, reader.GetInt32(2));
        for (var column = 3; column < reader.FieldCount; column++)
            Assert.Equal(1, reader.GetInt32(column));
    }

    [Theory]
    [InlineData("CREATE TABLE dbo.PrivilegeProbe (Id int);", 262)]
    [InlineData("ALTER TABLE dbo.UserAccounts ADD PrivilegeProbe int NULL;", 1088)]
    [InlineData("SELECT * FROM dbo.__EFMigrationsHistory;", 229)]
    [InlineData("DELETE FROM dbo.__EFMigrationsHistory;", 229)]
    [InlineData("CREATE DATABASE ForbiddenRuntimeDatabase;", 262)]
    [InlineData("CREATE LOGIN forbidden_runtime_login WITH PASSWORD = 'Unused-Test-Only-4321!';", 15247)]
    public async Task RuntimeIdentity_ShouldRejectSchemaHistoryAndServerChanges(string sql, int error)
    {
        await using var connection = await OpenRuntimeConnectionAsync();
        await using var command = new SqlCommand(sql, connection);
        var exception = await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(error, exception.Number);
    }

    [Fact]
    public async Task RuntimeIdentity_ShouldNotReadOtherDatabaseSecurityMetadata()
    {
        await using var connection = await OpenRuntimeConnectionAsync();
        await using var command = new SqlCommand("SELECT * FROM TutoringOtherDb.sys.tables;", connection);
        var exception = await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(916, exception.Number);
    }

    [Fact]
    public async Task MigrationIdentity_ShouldApplyAllMigrationsWithoutDatabaseOrServerOwnership()
    {
        await using var db = new TutoringDbContext(new DbContextOptionsBuilder<TutoringDbContext>()
            .UseSqlServer(Fixture.MigrationConnectionString).Options);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());

        await using var connection = new SqlConnection(Fixture.MigrationConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
            SELECT IS_SRVROLEMEMBER('sysadmin'), IS_MEMBER('db_owner'),
                HAS_PERMS_BY_NAME(NULL, NULL, 'ALTER ANY LOGIN'),
                HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'ALTER ANY USER'),
                HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE TABLE'),
                HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'ALTER'),
                HAS_PERMS_BY_NAME('messaging', 'SCHEMA', 'ALTER');
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        for (var column = 0; column < 4; column++)
            Assert.Equal(0, reader.GetInt32(column));
        for (var column = 4; column < 7; column++)
            Assert.Equal(1, reader.GetInt32(column));
    }

    private async Task<SqlConnection> OpenRuntimeConnectionAsync()
    {
        using var scope = Fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TutoringDbContext>();
        var connection = new SqlConnection(context.Database.GetConnectionString());
        await connection.OpenAsync();
        return connection;
    }
}
