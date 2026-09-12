using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.ResetPassword;

public sealed class ResetPasswordTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Email = "student@test.pl";
    private const string OldPassword = "Password123!";
    private const string NewPassword = "NewPassword456!";

    [Fact]
    public async Task ResetPassword_WithValidToken_ShouldReturnNoContent()
    {
        await RegisterAndConfirmStudentAsync(Email, OldPassword);
        await RequestPasswordResetAsync(Email);
        var resetEvent = await GetPasswordResetEventAsync(Email);

        var response = await ResetPasswordAsync(resetEvent.ResetToken, NewPassword);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_OldPasswordShouldNoLongerWork()
    {
        await RegisterAndConfirmStudentAsync(Email, OldPassword);
        await RequestPasswordResetAsync(Email);
        var resetEvent = await GetPasswordResetEventAsync(Email);

        await ResetPasswordAsync(resetEvent.ResetToken, NewPassword);

        var loginResponse = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email, Password = OldPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_NewPasswordShouldAllowLogin()
    {
        await RegisterAndConfirmStudentAsync(Email, OldPassword);
        await RequestPasswordResetAsync(Email);
        var resetEvent = await GetPasswordResetEventAsync(Email);

        await ResetPasswordAsync(resetEvent.ResetToken, NewPassword);

        var login = await LoginAsync(Email, NewPassword);

        Assert.False(string.IsNullOrWhiteSpace(login.Login.AccessToken));
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ShouldMarkTokenAsUsed()
    {
        await RegisterAndConfirmStudentAsync(Email, OldPassword);
        await RequestPasswordResetAsync(Email);
        var resetEvent = await GetPasswordResetEventAsync(Email);

        await ResetPasswordAsync(resetEvent.ResetToken, NewPassword);

        var account = await ExecuteDbAsync(dbContext =>
            dbContext.UserAccounts.SingleAsync(user =>
                user.Email.Value == Email));
        var token = await ExecuteDbAsync(dbContext =>
            dbContext.PasswordResetTokens.SingleAsync(item =>
                item.UserAccountId == account.Id));

        Assert.True(token.IsUsed);
    }

    [Fact]
    public async Task ResetPassword_WithAlreadyUsedToken_ShouldReturnUnauthorizedProblemDetails()
    {
        await RegisterAndConfirmStudentAsync(Email, OldPassword);
        await RequestPasswordResetAsync(Email);
        var resetEvent = await GetPasswordResetEventAsync(Email);

        await ResetPasswordAsync(resetEvent.ResetToken, NewPassword);

        var response = await ResetPasswordAsync(resetEvent.ResetToken, "AnotherPassword789!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(
            "Auth.InvalidPasswordResetToken",
            Assert.IsType<System.Text.Json.JsonElement>(
                problemDetails!.Extensions["code"]).GetString());
    }

    [Fact]
    public async Task ResetPassword_WithUnknownToken_ShouldReturnUnauthorizedProblemDetails()
    {
        var response = await ResetPasswordAsync("unknown-reset-token", NewPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(
            "Auth.InvalidPasswordResetToken",
            Assert.IsType<System.Text.Json.JsonElement>(
                problemDetails!.Extensions["code"]).GetString());
    }

    [Fact]
    public async Task ResetPassword_WithExpiredToken_ShouldReturnUnauthorizedProblemDetails()
    {
        await RegisterAndConfirmStudentAsync(Email, OldPassword);
        await RequestPasswordResetAsync(Email);
        var resetEvent = await GetPasswordResetEventAsync(Email);

        await ExecuteDbAsync(async dbContext =>
        {
            var token = await dbContext.PasswordResetTokens.SingleAsync();
            dbContext.Entry(token).Property(item => item.ExpiresAtUtc)
                .CurrentValue = DateTimeOffset.UtcNow.AddMinutes(-1);
            await dbContext.SaveChangesAsync();
        });

        var response = await ResetPasswordAsync(resetEvent.ResetToken, NewPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(
            "Auth.InvalidPasswordResetToken",
            Assert.IsType<System.Text.Json.JsonElement>(
                problemDetails!.Extensions["code"]).GetString());
    }

    [Fact]
    public async Task ResetPassword_WithInvalidatedToken_ShouldReturnUnauthorizedProblemDetails()
    {
        await RegisterAndConfirmStudentAsync(Email, OldPassword);

        await RequestPasswordResetAsync(Email);
        var firstResetEvent = await GetPasswordResetEventAsync(Email);

        // second request invalidates the first token
        await RequestPasswordResetAsync(Email);

        var response = await ResetPasswordAsync(firstResetEvent.ResetToken, NewPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(
            "Auth.InvalidPasswordResetToken",
            Assert.IsType<System.Text.Json.JsonElement>(
                problemDetails!.Extensions["code"]).GetString());
    }

    [Fact]
    public async Task ResetPassword_WithPasswordViolatingPolicy_ShouldReturnBadRequest()
    {
        await RegisterAndConfirmStudentAsync(Email, OldPassword);
        await RequestPasswordResetAsync(Email);
        var resetEvent = await GetPasswordResetEventAsync(Email);

        var response = await ResetPasswordAsync(resetEvent.ResetToken, "weak");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ShouldRevokeAllActiveRefreshTokens()
    {
        await RegisterAndConfirmStudentAsync(Email, OldPassword);

        await LoginAsync(Email, OldPassword);
        await LoginAsync(Email, OldPassword);

        await RequestPasswordResetAsync(Email);
        var resetEvent = await GetPasswordResetEventAsync(Email);

        await ResetPasswordAsync(resetEvent.ResetToken, NewPassword);

        var account = await ExecuteDbAsync(dbContext =>
            dbContext.UserAccounts.SingleAsync(user =>
                user.Email.Value == Email));

        var activeRefreshTokens = await ExecuteDbAsync(dbContext =>
            dbContext.RefreshTokens.CountAsync(refreshToken =>
                refreshToken.UserAccountId == account.Id &&
                refreshToken.RevokedAtUtc == null));

        Assert.Equal(0, activeRefreshTokens);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ShouldInvalidateOtherActiveResetTokensForUser()
    {
        await RegisterAndConfirmStudentAsync(Email, OldPassword);

        await RequestPasswordResetAsync(Email);
        var resetEvent = await GetPasswordResetEventAsync(Email);

        var account = await ExecuteDbAsync(dbContext =>
            dbContext.UserAccounts.SingleAsync(user =>
                user.Email.Value == Email));

        // A second active token is created directly to simulate another
        // outstanding reset request that should be invalidated once
        // the first one is successfully consumed.
        const string otherTokenHash = "some-other-active-token-hash";
        await ExecuteDbAsync(async dbContext =>
        {
            var extraToken = new Tutoring.Infrastructure.Authentication.PasswordResetToken(
                account.Id,
                otherTokenHash,
                DateTimeOffset.UtcNow.AddHours(1));

            dbContext.PasswordResetTokens.Add(extraToken);
            await dbContext.SaveChangesAsync();
        });

        await ResetPasswordAsync(resetEvent.ResetToken, NewPassword);

        var otherToken = await ExecuteDbAsync(dbContext =>
            dbContext.PasswordResetTokens.SingleAsync(item =>
                item.TokenHash == otherTokenHash));

        Assert.True(otherToken.IsInvalidated);
        Assert.False(otherToken.IsUsed);
    }
}
