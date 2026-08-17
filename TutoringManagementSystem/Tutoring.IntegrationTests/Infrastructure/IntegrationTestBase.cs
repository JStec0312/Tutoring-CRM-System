using Microsoft.Extensions.DependencyInjection;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase
    : IClassFixture<IntegrationTestFixture>,
      IAsyncLifetime
{
    protected IntegrationTestFixture Fixture { get; }

    protected HttpClient Client => Fixture.Client;

    protected IntegrationTestBase(
        IntegrationTestFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await Fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected async Task<T> ExecuteDbAsync<T>(
        Func<TutoringDbContext, Task<T>> action)
    {
        using var scope =
            Fixture.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<TutoringDbContext>();

        return await action(dbContext);
    }

    protected async Task ExecuteDbAsync(
        Func<TutoringDbContext, Task> action)
    {
        using var scope =
            Fixture.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<TutoringDbContext>();

        await action(dbContext);
    }
}