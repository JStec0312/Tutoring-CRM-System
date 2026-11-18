using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Google;
using Tutoring.Infrastructure.Authentication.Passwords;
using Tutoring.Infrastructure.Mailing;
using Tutoring.Infrastructure.Mailing.Handlers;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.RequestPasswordReset;

[Collection(IntegrationTestCollection.Name)]
public sealed class RequestPasswordResetTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task RequestPasswordReset_WithExistingActiveUser_ShouldReturnNoContent()
    {
        const string email = "student@test.pl";

        await RegisterAndConfirmStudentAsync(email, "Password123!");

        var response = await RequestPasswordResetAsync(email);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RequestPasswordReset_WithExistingActiveUser_ShouldCreatePasswordResetTokenWithHashedValue()
    {
        const string email = "student@test.pl";

        await RegisterAndConfirmStudentAsync(email, "Password123!");

        var response = await RequestPasswordResetAsync(email);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var resetEvent = await GetPasswordResetEventAsync(email);

        var account = await ExecuteDbAsync(dbContext =>
            dbContext.UserAccounts.SingleAsync(user =>
                user.Email.Value == email));
        var token = await ExecuteDbAsync(dbContext =>
            dbContext.PasswordResetTokens.SingleAsync(item =>
                item.UserAccountId == account.Id));

        Assert.False(token.IsUsed);
        Assert.False(token.IsInvalidated);
        Assert.NotEqual(resetEvent.ResetToken, token.TokenHash);
        Assert.DoesNotContain(resetEvent.ResetToken, token.TokenHash);
    }

    [Fact]
    public async Task RequestPasswordReset_WithExistingActiveUser_ShouldCreateOutboxMessageWithEventDetails()
    {
        const string email = "student@test.pl";

        await RegisterAndConfirmStudentAsync(email, "Password123!");

        var response = await RequestPasswordResetAsync(email);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var account = await ExecuteDbAsync(dbContext =>
            dbContext.UserAccounts.SingleAsync(user =>
                user.Email.Value == email));

        var resetEvent = await GetPasswordResetEventAsync(email);

        Assert.Equal(account.Id.Value, resetEvent.UserId);
        Assert.Equal(email, resetEvent.Email);
        Assert.False(string.IsNullOrWhiteSpace(resetEvent.ResetToken));
    }

    [Fact]
    public async Task RequestPasswordReset_WithActiveUser_ShouldRenderLinkToConfiguredFrontend()
    {
        const string email = "student@test.pl";
        await RegisterAndConfirmStudentAsync(email, "Password123!");
        var response = await RequestPasswordResetAsync(email);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var resetEvent = await GetPasswordResetEventAsync(email);
        var mailer = new CapturingMailer();
        var options = Fixture.Services.GetRequiredService<IOptions<PasswordResetOptions>>();
        var handler = new PasswordResetRequestedEmailHandler(mailer, options);

        await handler.HandleAsync(JsonSerializer.Serialize(resetEvent), CancellationToken.None);

        Assert.Equal(email, mailer.Recipient);
        var expectedUrl = $"https://test.local/reset-password?token={Uri.EscapeDataString(resetEvent.ResetToken)}";
        Assert.NotNull(mailer.HtmlBody);
        Assert.Contains($"href=\"{expectedUrl}\"", mailer.HtmlBody);
    }

    [Fact]
    public async Task RequestPasswordReset_WithUnknownEmail_ShouldReturnSameStatusAsExistingUser()
    {
        var response = await RequestPasswordResetAsync("unknown@test.pl");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RequestPasswordReset_WithUnknownEmail_ShouldNotCreateTokenOrOutboxMessage()
    {
        const string email = "unknown@test.pl";

        var response = await RequestPasswordResetAsync(email);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var tokenCount = await ExecuteDbAsync(dbContext =>
            dbContext.PasswordResetTokens.CountAsync());
        var outboxCount = await ExecuteDbAsync(dbContext =>
            dbContext.OutboxMessages.CountAsync(message =>
                message.Type ==
                    Tutoring.Infrastructure.Messaging.Contracts
                        .PasswordResetRequestedIntegrationEvent.EventType));

        Assert.Equal(0, tokenCount);
        Assert.Equal(0, outboxCount);
    }

    [Fact]
    public async Task RequestPasswordReset_CalledTwice_ShouldInvalidatePreviousActiveToken()
    {
        const string email = "student@test.pl";

        await RegisterAndConfirmStudentAsync(email, "Password123!");

        await RequestPasswordResetAsync(email);
        var account = await ExecuteDbAsync(dbContext =>
            dbContext.UserAccounts.SingleAsync(user =>
                user.Email.Value == email));
        var firstToken = await ExecuteDbAsync(dbContext =>
            dbContext.PasswordResetTokens.SingleAsync(item =>
                item.UserAccountId == account.Id));

        await RequestPasswordResetAsync(email);

        var tokens = await ExecuteDbAsync(dbContext =>
            dbContext.PasswordResetTokens
                .Where(item => item.UserAccountId == account.Id)
                .ToListAsync());

        Assert.Equal(2, tokens.Count);

        var invalidatedFirstToken = tokens.Single(item => item.Id == firstToken.Id);
        var activeToken = tokens.Single(item => item.Id != firstToken.Id);

        Assert.True(invalidatedFirstToken.IsInvalidated);
        Assert.False(activeToken.IsInvalidated);
        Assert.False(activeToken.IsUsed);
    }

    [Fact]
    public async Task RequestPasswordReset_WithInactiveAccount_ShouldNotCreateTokenOrOutboxMessage()
    {
        const string email = "student@test.pl";

        var registrationResponse = await Client.PostAsJsonAsync(
            "/api/auth/register/student",
            new
            {
                Email = email,
                Username = "student1",
                Password = "Password123!"
            });
        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        var response = await RequestPasswordResetAsync(email);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var tokenCount = await ExecuteDbAsync(dbContext =>
            dbContext.PasswordResetTokens.CountAsync());
        var outboxCount = await ExecuteDbAsync(dbContext =>
            dbContext.OutboxMessages.CountAsync(message =>
                message.Type ==
                    Tutoring.Infrastructure.Messaging.Contracts
                        .PasswordResetRequestedIntegrationEvent.EventType));

        Assert.Equal(0, tokenCount);
        Assert.Equal(0, outboxCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RequestPasswordReset_WithGoogleAccountWithoutPassword_ShouldNotCreateReset(bool active)
    {
        const string email = "google-only@test.pl";
        await ExecuteDbAsync(async db =>
        {
            var account = new UserAccount(
                new EmailAddress(email), null,
                new PersonalProfile("google-only", null, null, null), DateTimeOffset.UtcNow);
            if (active)
                account.Activate();

            db.UserAccounts.Add(account);
            db.GoogleSignInAccountLinks.Add(new GoogleSignInAccountLink("google-only-subject", account.Id));
            await db.SaveChangesAsync();
        });

        using var response = await RequestPasswordResetAsync(email);

        if (active)
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            await AssertProblemCodeAsync(response, "Auth.GoogleAccountHasNoPassword");
        }
        else
        {
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        Assert.False(await ExecuteDbAsync(db => db.PasswordResetTokens.AnyAsync()));
        Assert.False(await ExecuteDbAsync(db => db.OutboxMessages.AnyAsync()));
    }

    [Fact]
    public async Task RequestPasswordReset_WithGoogleLinkedAccountWithPassword_ShouldCreateReset()
    {
        const string email = "linked@test.pl";
        await RegisterAndConfirmStudentAsync(email, "Password123!");
        await ExecuteDbAsync(async db =>
        {
            var account = await db.UserAccounts.SingleAsync(user => user.Email.Value == email);
            db.GoogleSignInAccountLinks.Add(new GoogleSignInAccountLink("linked-subject", account.Id));
            await db.SaveChangesAsync();
        });

        using var response = await RequestPasswordResetAsync(email);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, await ExecuteDbAsync(db => db.PasswordResetTokens.CountAsync()));
        var resetEvent = await GetPasswordResetEventAsync(email);
        Assert.Equal(email, resetEvent.Email);
    }

    private sealed class CapturingMailer : IMailer
    {
        public string? Recipient { get; private set; }
        public string? HtmlBody { get; private set; }

        public Task SendAsync(
            string recipient,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken)
        {
            Recipient = recipient;
            HtmlBody = htmlBody;
            return Task.CompletedTask;
        }
    }
}
