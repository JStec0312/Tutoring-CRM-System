using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.ConfirmEmail;

public sealed class ConfirmEmailTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task ConfirmEmail_AfterRegistration_ShouldLeaveAccountPendingAndCreateVerificationToken()
    {
        const string email = "student@test.pl";

        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/student",
            new
            {
                Email = email,
                Username = "student1",
                Password = "Password123!"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var account = await ExecuteDbAsync(dbContext =>
            dbContext.UserAccounts.SingleAsync(account =>
                account.Email.Value == email));
        var verificationToken = await ExecuteDbAsync(dbContext =>
            dbContext.EmailVerificationTokens.SingleAsync(token =>
                token.UserAccountId == account.Id));

        Assert.Equal(AccountStatus.PendingActivation, account.Status);
        Assert.Equal(account.Id, verificationToken.UserAccountId);
        Assert.False(verificationToken.IsUsed);
        Assert.NotNull(await GetVerificationTokenAsync(email));
    }

    [Fact]
    public async Task ConfirmEmail_WithValidToken_ShouldActivateAccountAndMarkTokenAsUsed()
    {
        const string email = "student@test.pl";

        await RegisterAndConfirmStudentAsync(email, "Password123!");

        var account = await ExecuteDbAsync(dbContext =>
            dbContext.UserAccounts.SingleAsync(account =>
                account.Email.Value == email));
        var verificationToken = await ExecuteDbAsync(dbContext =>
            dbContext.EmailVerificationTokens.SingleAsync(token =>
                token.UserAccountId == account.Id));

        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.NotNull(verificationToken.UsedAtUtc);
        Assert.True(verificationToken.IsUsed);
    }

    [Fact]
    public async Task ConfirmEmail_WithUnknownToken_ShouldReturnUnauthorizedProblemDetails()
    {
        var response = await Client.GetAsync(
            "/api/auth/email/confirm?token=unknown-verification-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal("Unauthorized", problemDetails!.Title);
        Assert.Equal(
            "Auth.InvalidEmailVerificationToken",
            Assert.IsType<JsonElement>(
                problemDetails.Extensions["code"]).GetString());
    }

    [Fact]
    public async Task ConfirmEmail_WithPreviouslyUsedToken_ShouldReturnUnauthorizedProblemDetails()
    {
        const string email = "student@test.pl";

        await RegisterAndConfirmStudentAsync(email, "Password123!");
        var verificationToken = await GetVerificationTokenAsync(email);

        var response = await Client.GetAsync(
            $"/api/auth/email/confirm?token={Uri.EscapeDataString(verificationToken)}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(
            "Auth.InvalidEmailVerificationToken",
            Assert.IsType<JsonElement>(
                problemDetails!.Extensions["code"]).GetString());
    }

    [Fact]
    public async Task Login_WithUnconfirmedAccount_ShouldReturnForbiddenProblemDetails()
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

        var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = email, Password = "Password123!" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal("Forbidden", problemDetails!.Title);
        Assert.Equal(
            "Auth.InactiveAccount.",
            Assert.IsType<JsonElement>(
                problemDetails.Extensions["code"]).GetString());
    }

    [Fact]
    public async Task Login_AfterEmailConfirmation_ShouldReturnAccessToken()
    {
        const string email = "student@test.pl";

        await RegisterAndConfirmStudentAsync(email, "Password123!");

        var login = await LoginAsync(email, "Password123!");

        Assert.False(string.IsNullOrWhiteSpace(login.Login.AccessToken));
    }

    [Fact]
    public async Task ConfirmEmail_WithExpiredToken_ShouldReturnUnauthorizedProblemDetails()
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

        var verificationToken = await GetVerificationTokenAsync(email);
        await ExecuteDbAsync(async dbContext =>
        {
            var token = await dbContext.EmailVerificationTokens.SingleAsync();
            dbContext.Entry(token).Property(item => item.ExpiresAtUtc)
                .CurrentValue = DateTimeOffset.UtcNow.AddMinutes(-1);
            await dbContext.SaveChangesAsync();
        });

        var response = await Client.GetAsync(
            $"/api/auth/email/confirm?token={Uri.EscapeDataString(verificationToken)}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(
            "Auth.InvalidEmailVerificationToken",
            Assert.IsType<JsonElement>(
                problemDetails!.Extensions["code"]).GetString());
    }
}
