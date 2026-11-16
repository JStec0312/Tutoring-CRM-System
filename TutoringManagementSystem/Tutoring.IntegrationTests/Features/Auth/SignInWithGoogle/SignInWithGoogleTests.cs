using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tutoring.Api.Features.Auth.SignInWithGoogle;
using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Google;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.SignInWithGoogle;

[Collection(IntegrationTestCollection.Name)]
public sealed class SignInWithGoogleTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Endpoint = "/api/auth/google/sign-in";
    private const string Subject = "google-subject";
    private const string Email = "google@example.com";
    private static readonly string Nonce = new('A', 64);
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SignIn_LinkedSubject_IssuesSessionForCrmAccount(bool useNativeSession)
    {
        var accountId = await SeedAccountAsync(Email, linked: true);
        await SeedChallengeAsync(Now.AddMinutes(5));
        using var factory = Fixture.WithTimeProvider(new FixedTimeProvider());
        using var verifierFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Replace(ServiceDescriptor.Singleton<IGoogleSignInTokenVerifier>(
                new StubVerifier(new VerifiedGoogleSignInIdentity(Subject, Nonce, "changed@gmail.com"))))));
        using var client = verifierFactory.CreateClient();

        using var response = await client.PostAsJsonAsync(Endpoint, new SignInWithGoogleRequest("valid", useNativeSession));

        var result = await ReadSuccessAsync(response);
        Assert.Equal("signedIn", result.Status);
        Assert.Null(result.ContinuationToken);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Equal(accountId.Value.ToString(), jwt.Subject);
        Assert.Contains(jwt.Claims, claim => claim.Type == "role" && claim.Value == nameof(UserRole.Tutor));

        if (useNativeSession)
        {
            Assert.NotNull(result.RefreshToken);
            Assert.NotNull(result.RefreshTokenExpiresAt);
            Assert.False(response.Headers.Contains("Set-Cookie"));
        }
        else
        {
            Assert.Null(result.RefreshToken);
            Assert.NotEmpty(ExtractRefreshToken(response));
            Assert.Contains(response.Headers.GetValues("Set-Cookie"),
                cookie => cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        }

        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.False(await ExecuteDbAsync(db => db.GoogleAuthenticationChallenges.AnyAsync()));
        Assert.Equal(accountId, (await ExecuteDbAsync(db => db.RefreshTokens.SingleAsync())).UserAccountId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SignIn_UnlinkedSubject_ReturnsContinuationForExpectedAction(bool existingAccount)
    {
        UserAccountId? accountId = existingAccount ? await SeedAccountAsync(Email) : null;
        await SeedChallengeAsync(Now.AddMinutes(5));
        using var factory = Fixture.WithTimeProvider(new FixedTimeProvider());
        using var verifierFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Replace(ServiceDescriptor.Singleton<IGoogleSignInTokenVerifier>(
                new StubVerifier(new VerifiedGoogleSignInIdentity(Subject, Nonce, Email))))));
        using var client = verifierFactory.CreateClient();

        using var response = await client.PostAsJsonAsync(Endpoint, new SignInWithGoogleRequest("valid"));

        var result = await ReadSuccessAsync(response);
        Assert.Equal(existingAccount ? "linkRequired" : "registrationRequired", result.Status);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.NotNull(result.ContinuationToken);
        Assert.Equal(Now.AddMinutes(5), result.ContinuationExpiresAtUtc);
        var continuation = await ExecuteDbAsync(db => db.GoogleSignInContinuations.SingleAsync());
        Assert.Equal(GoogleSignInContinuation.HashToken(result.ContinuationToken), continuation.TokenHash);
        Assert.NotEqual(result.ContinuationToken, continuation.TokenHash);
        Assert.Equal(accountId, continuation.AccountId);
        Assert.Equal(Subject, continuation.GoogleSubject);
        Assert.Equal(Email, continuation.Email);
        Assert.False(await ExecuteDbAsync(db => db.GoogleAuthenticationChallenges.AnyAsync()));
        Assert.False(await ExecuteDbAsync(db => db.GoogleSignInAccountLinks.AnyAsync()));
        Assert.False(await ExecuteDbAsync(db => db.RefreshTokens.AnyAsync()));
    }

    [Fact]
    public async Task SignIn_ConcurrentReplay_CreatesOnlyOneSession()
    {
        await SeedAccountAsync(Email, linked: true);
        await SeedChallengeAsync(Now.AddMinutes(5));
        using var factory = Fixture.WithTimeProvider(new FixedTimeProvider());
        using var verifierFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Replace(ServiceDescriptor.Singleton<IGoogleSignInTokenVerifier>(
                new StubVerifier(new VerifiedGoogleSignInIdentity(Subject, Nonce, Email))))));
        using var client = verifierFactory.CreateClient();

        var request = new SignInWithGoogleRequest("valid");
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync(Endpoint, request),
            client.PostAsJsonAsync(Endpoint, request));
        using var success = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        using var replay = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);

        await AssertProblemCodeAsync(replay, "Auth.InvalidGoogleSignIn");
        Assert.Equal(1, await ExecuteDbAsync(db => db.RefreshTokens.CountAsync()));
    }

    [Theory]
    [InlineData("invalid", true, true, HttpStatusCode.Unauthorized, "Auth.InvalidGoogleSignIn")]
    [InlineData("unavailable", true, true, HttpStatusCode.ServiceUnavailable, "Auth.GoogleSignInUnavailable")]
    [InlineData("valid", false, true, HttpStatusCode.Unauthorized, "Auth.InvalidGoogleSignIn")]
    [InlineData("valid", true, false, HttpStatusCode.Unauthorized, "Auth.GoogleVerifiedEmailRequired")]
    public async Task SignIn_Failure_DoesNotIssueSessionOrContinuation(
        string token, bool challengeIsCurrent, bool emailVerified, HttpStatusCode status, string code)
    {
        await SeedChallengeAsync(challengeIsCurrent ? Now.AddMinutes(5) : Now);
        using var factory = Fixture.WithTimeProvider(new FixedTimeProvider());
        using var verifierFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Replace(ServiceDescriptor.Singleton<IGoogleSignInTokenVerifier>(
                new StubVerifier(new VerifiedGoogleSignInIdentity(Subject, Nonce, emailVerified ? Email : null))))));
        using var client = verifierFactory.CreateClient();

        using var response = await client.PostAsJsonAsync(Endpoint, new SignInWithGoogleRequest(token));

        Assert.Equal(status, response.StatusCode);
        await AssertProblemCodeAsync(response, code);
        Assert.Equal(1, await ExecuteDbAsync(db => db.GoogleAuthenticationChallenges.CountAsync()));
        Assert.False(await ExecuteDbAsync(db => db.RefreshTokens.AnyAsync()));
        Assert.False(await ExecuteDbAsync(db => db.GoogleSignInContinuations.AnyAsync()));
    }

    [Fact]
    public async Task SignIn_InactiveLinkedAccount_IsForbidden()
    {
        await SeedAccountAsync(Email, linked: true, active: false);
        await SeedChallengeAsync(Now.AddMinutes(5));
        using var factory = Fixture.WithTimeProvider(new FixedTimeProvider());
        using var verifierFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Replace(ServiceDescriptor.Singleton<IGoogleSignInTokenVerifier>(
                new StubVerifier(new VerifiedGoogleSignInIdentity(Subject, Nonce, Email))))));
        using var client = verifierFactory.CreateClient();

        using var response = await client.PostAsJsonAsync(Endpoint, new SignInWithGoogleRequest("valid"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(await ExecuteDbAsync(db => db.RefreshTokens.AnyAsync()));
    }

    private Task SeedChallengeAsync(DateTimeOffset expiresAtUtc) => ExecuteDbAsync(async db =>
    {
        db.GoogleAuthenticationChallenges.Add(new GoogleAuthenticationChallenge(Nonce, expiresAtUtc));
        await db.SaveChangesAsync();
    });

    private Task<UserAccountId> SeedAccountAsync(string email, bool linked = false, bool active = true) =>
        ExecuteDbAsync(async db =>
        {
            var account = new UserAccount(
                new EmailAddress(email), new PasswordHash("seeded-hash"),
                new PersonalProfile("google-test", "Google", "Test", null), Now);
            account.AssignRole(UserRole.Tutor);
            if (active)
                account.Activate();

            db.UserAccounts.Add(account);
            if (linked)
                db.GoogleSignInAccountLinks.Add(new GoogleSignInAccountLink(Subject, account.Id));

            await db.SaveChangesAsync();
            return account.Id;
        });

    private static async Task<SignInWithGoogleResponse> ReadSuccessAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<SignInWithGoogleResponse>(
            await response.Content.ReadFromJsonAsync<SignInWithGoogleResponse>());
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class StubVerifier(VerifiedGoogleSignInIdentity identity) : IGoogleSignInTokenVerifier
    {
        public Task<VerifiedGoogleSignInIdentity> VerifyAsync(string idToken, CancellationToken cancellationToken) =>
            idToken switch
            {
                "valid" => Task.FromResult(identity),
                "unavailable" => throw new GoogleSignInVerificationUnavailableException(),
                _ => throw new InvalidGoogleSignInTokenException()
            };
    }
}
