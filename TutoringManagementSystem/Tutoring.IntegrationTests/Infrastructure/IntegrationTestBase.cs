using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tutoring.Api.Features.Auth.Login;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Mailing;
using Tutoring.Infrastructure.Messaging.Contracts;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase
    : IClassFixture<IntegrationTestFixture>,
      IAsyncLifetime
{
    protected sealed record AuthenticatedSession(
        LoginResponse Login,
        string RefreshToken);

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

    protected async Task<HttpResponseMessage> RequestPasswordResetAsync(string email)
    {
        return await Client.PostAsJsonAsync(
            "/api/auth/password-reset/request",
            new { Email = email });
    }

    protected async Task<PasswordResetRequestedIntegrationEvent> GetPasswordResetEventAsync(string email)
    {
        var payloads = await ExecuteDbAsync(dbContext =>
            dbContext.OutboxMessages
                .AsNoTracking()
                .Where(message =>
                    message.Type == PasswordResetRequestedIntegrationEvent.EventType)
                .Select(message => message.Payload)
                .ToListAsync());

        var resetEvent = payloads
            .Select(payload =>
                JsonSerializer.Deserialize<PasswordResetRequestedIntegrationEvent>(
                    payload))
            .LastOrDefault(@event => @event?.Email == email);

        Assert.NotNull(resetEvent);
        Assert.False(string.IsNullOrWhiteSpace(resetEvent!.ResetToken));

        return resetEvent;
    }

    protected async Task<HttpResponseMessage> ResetPasswordAsync(
        string token,
        string newPassword)
    {
        return await Client.PostAsJsonAsync(
            "/api/auth/password-reset",
            new { Token = token, NewPassword = newPassword });
    }

    protected async Task<AuthenticatedSession> LoginAsync(
        string email,
        string password)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = email,
                Password = password
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login!.AccessToken));

        return new AuthenticatedSession(
            login,
            ExtractRefreshToken(response));
    }

    protected HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string requestUri,
        string accessToken,
        object? content = null)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);

        if (content is not null)
        {
            request.Content = JsonContent.Create(content);
        }

        return request;
    }

    protected async Task<HttpResponseMessage> SendWithRefreshTokenAsync(
        HttpMethod method,
        string requestUri,
        string? refreshToken)
    {
        using var request = new HttpRequestMessage(method, requestUri);

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            request.Headers.Add(
                "Cookie",
                $"{RefreshTokenCookieName}={refreshToken}");
        }

        return await Client.SendAsync(request);
    }

    protected string ExtractRefreshToken(HttpResponseMessage response)
    {
        var setCookie = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(value =>
                value.StartsWith(
                    $"{RefreshTokenCookieName}=",
                    StringComparison.OrdinalIgnoreCase))
            : null;

        Assert.NotNull(setCookie);

        var cookiePair = setCookie!.Split(';')[0];
        return cookiePair.Split('=', 2)[1];
    }

    protected bool HasRefreshTokenDeletionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return false;
        }

        var options = Fixture.Services
            .GetRequiredService<IOptions<RefreshTokenOptions>>()
            .Value;
        var sameSite = options.SameSiteRefreshTokenCookie.ToLowerInvariant();

        return values.Any(value =>
            value.Contains(
                $"{RefreshTokenCookieName}=",
                StringComparison.OrdinalIgnoreCase) &&
            value.Contains(
                $"path={options.RefreshTokenPath}",
                StringComparison.OrdinalIgnoreCase) &&
            value.Contains(
                $"samesite={sameSite}",
                StringComparison.OrdinalIgnoreCase) &&
            (!options.SecureRefreshTokenCookie ||
                value.Contains("secure", StringComparison.OrdinalIgnoreCase)));
    }

    protected string RefreshTokenCookieName =>
        Fixture.Services
            .GetRequiredService<IOptions<RefreshTokenOptions>>()
            .Value
            .RefreshTokenCookieName;
}