using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tutoring.Infrastructure.Mailing;
using Tutoring.Infrastructure.Messaging.Contracts;
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

    protected async Task RegisterAndConfirmStudentAsync(
        string email,
        string password)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/student",
            new
            {
                Email = email,
                Username = email.Split('@')[0],
                Password = password
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var verificationToken = await GetVerificationTokenAsync(email);
        await ConfirmEmailAsync(verificationToken);
    }

    protected async Task<string> GetVerificationTokenAsync(string email)
    {
        var payloads = await ExecuteDbAsync(dbContext =>
            dbContext.OutboxMessages
                .AsNoTracking()
                .Where(message =>
                    message.Type == UserRegisteredIntegrationEvent.EventType)
                .Select(message => message.Payload)
                .ToListAsync());

        var registrationEvent = payloads
            .Select(payload =>
                JsonSerializer.Deserialize<UserRegisteredIntegrationEvent>(
                    payload))
            .SingleOrDefault(@event => @event?.Email == email);

        Assert.NotNull(registrationEvent);
        Assert.False(string.IsNullOrWhiteSpace(
            registrationEvent!.VerificationToken));

        return registrationEvent.VerificationToken;
    }

    protected async Task ConfirmEmailAsync(string verificationToken)
    {
        var response = await Client.GetAsync(
            $"/api/auth/email/confirm?token={Uri.EscapeDataString(verificationToken)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}