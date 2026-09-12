using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.RequestPasswordReset;

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
}
