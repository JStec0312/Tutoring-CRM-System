using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.RegisterWithGoogle;
using Tutoring.Api.Features.Auth.SignInWithGoogle;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Google;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.RegisterWithGoogle;

[Collection(IntegrationTestCollection.Name)]
public sealed class RegisterWithGoogleTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Endpoint = "/api/auth/google/register";
    private const string Email = "new-google@example.com";
    private const string Subject = "google-registration-subject";
    private const string Password = "StrongPassword123!";
    private static readonly string Token = new('A', 64);

    [Theory]
    [InlineData("Tutor", false)]
    [InlineData("Student", true)]
    public async Task Register_VerifiedGoogleEmail_CreatesAccountAndSession(string role, bool useNativeSession)
    {
        await SeedContinuationAsync();
        var request = CreateRequest() with { Role = role, UseNativeSession = useNativeSession };

        using var response = await Client.PostAsJsonAsync(Endpoint, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = await ReadResponseAsync(response);
        Assert.Equal("signedIn", session.Status);
        Assert.True(response.Headers.CacheControl?.NoStore);
        if (useNativeSession)
        {
            Assert.NotNull(session.RefreshToken);
            Assert.False(response.Headers.Contains("Set-Cookie"));
        }
        else
        {
            Assert.Null(session.RefreshToken);
            Assert.NotEmpty(ExtractRefreshToken(response));
        }

        var account = await ExecuteDbAsync(db => db.UserAccounts.Include(user => user.RoleAssignments).SingleAsync());
        Assert.Equal(Email, account.Email.Value);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Null(account.PasswordHash);
        Assert.Equal(role, Assert.Single(account.Roles).ToString());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(session.AccessToken);
        Assert.Equal(account.Id.Value.ToString(), jwt.Subject);
        Assert.Contains(jwt.Claims, claim => claim.Type == "role" && claim.Value == role);
        Assert.Equal(account.Id, (await ExecuteDbAsync(db => db.GoogleSignInAccountLinks.SingleAsync())).AccountId);
        Assert.Equal(account.Id, (await ExecuteDbAsync(db => db.RefreshTokens.SingleAsync())).UserAccountId);
        Assert.Equal(role == "Tutor", await ExecuteDbAsync(db => db.Tutors.AnyAsync()));
        Assert.Equal(role == "Student", await ExecuteDbAsync(db => db.Students.AnyAsync()));
        Assert.False(await ExecuteDbAsync(db => db.GoogleSignInContinuations.AnyAsync()));
        Assert.False(await ExecuteDbAsync(db => db.OutboxMessages.AnyAsync()));

        using var passwordLogin = await Client.PostAsJsonAsync("/api/auth/login", new { Email, Password });
        Assert.Equal(HttpStatusCode.Unauthorized, passwordLogin.StatusCode);
        await AssertProblemCodeAsync(passwordLogin, "Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Register_ConcurrentRequests_CreateOnlyOneAccountAndSession()
    {
        await SeedContinuationAsync();
        var request = CreateRequest();

        var responses = await Task.WhenAll(
            Client.PostAsJsonAsync(Endpoint, request),
            Client.PostAsJsonAsync(Endpoint, request));
        using var success = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        using var rejected = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);

        await AssertProblemCodeAsync(rejected, "Auth.InvalidGoogleRegistrationContinuation");
        Assert.Equal(1, await ExecuteDbAsync(db => db.UserAccounts.CountAsync()));
        Assert.Equal(1, await ExecuteDbAsync(db => db.GoogleSignInAccountLinks.CountAsync()));
        Assert.Equal(1, await ExecuteDbAsync(db => db.RefreshTokens.CountAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Register_ExpiredOrLinkOnlyContinuation_IsRejected(bool linkOnly)
    {
        UserAccountId? accountId = null;
        if (linkOnly)
        {
            await RegisterAndConfirmStudentAsync(Email, Password);
            accountId = await ExecuteDbAsync(db => db.UserAccounts.Select(user => user.Id).SingleAsync());
        }
        await SeedContinuationAsync(accountId, expired: !linkOnly);

        using var response = await Client.PostAsJsonAsync(Endpoint, CreateRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemCodeAsync(response, "Auth.InvalidGoogleRegistrationContinuation");
        Assert.False(await ExecuteDbAsync(db => db.GoogleSignInAccountLinks.AnyAsync()));
        Assert.False(await ExecuteDbAsync(db => db.RefreshTokens.AnyAsync()));
    }

    [Fact]
    public async Task Register_ExistingEmail_DoesNotAttachGoogleToCrmAccount()
    {
        await SeedContinuationAsync();
        await RegisterAndConfirmStudentAsync(Email, Password);

        using var response = await Client.PostAsJsonAsync(Endpoint, CreateRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemCodeAsync(response, "Auth.GoogleRegistrationConflict");
        Assert.Equal(1, await ExecuteDbAsync(db => db.UserAccounts.CountAsync()));
        Assert.False(await ExecuteDbAsync(db => db.GoogleSignInAccountLinks.AnyAsync()));
        Assert.Equal(1, await ExecuteDbAsync(db => db.GoogleSignInContinuations.CountAsync()));
    }

    [Fact]
    public async Task Register_AdministratorRole_IsRejected()
    {
        await SeedContinuationAsync();

        using var response = await Client.PostAsJsonAsync(Endpoint, CreateRequest() with { Role = "Administrator" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(await ExecuteDbAsync(db => db.UserAccounts.AnyAsync()));
        Assert.Equal(1, await ExecuteDbAsync(db => db.GoogleSignInContinuations.CountAsync()));
    }

    [Fact]
    public async Task Register_PasswordlessAccount_CanSetPasswordThroughReset()
    {
        await SeedContinuationAsync();
        using var registration = await Client.PostAsJsonAsync(Endpoint, CreateRequest());
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using var resetRequest = await RequestPasswordResetAsync(Email);
        Assert.True(resetRequest.IsSuccessStatusCode);
        var resetEvent = await GetPasswordResetEventAsync(Email);
        using var reset = await ResetPasswordAsync(resetEvent.ResetToken, Password);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        var session = await LoginAsync(Email, Password);
        Assert.NotEmpty(session.Login.AccessToken);
    }

    private static RegisterWithGoogleRequest CreateRequest() =>
        new(Token, "Tutor", "googleUser", "Test", "User", null);

    private static async Task<SignInWithGoogleResponse> ReadResponseAsync(HttpResponseMessage response) =>
        Assert.IsType<SignInWithGoogleResponse>(
            await response.Content.ReadFromJsonAsync<SignInWithGoogleResponse>());

    private Task SeedContinuationAsync(UserAccountId? accountId = null, bool expired = false) =>
        ExecuteDbAsync(async db =>
        {
            db.GoogleSignInContinuations.Add(new GoogleSignInContinuation(
                GoogleSignInContinuation.HashToken(Token), Subject, Email,
                accountId, DateTimeOffset.UtcNow.AddMinutes(expired ? -1 : 5)));
            await db.SaveChangesAsync();
        });
}
