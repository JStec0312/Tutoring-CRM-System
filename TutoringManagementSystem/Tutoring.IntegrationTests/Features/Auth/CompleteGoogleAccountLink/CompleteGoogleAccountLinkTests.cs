using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tutoring.Api.Features.Auth.CompleteGoogleAccountLink;
using Tutoring.Api.Features.Auth.CreateGoogleSignInChallenge;
using Tutoring.Api.Features.Auth.SignInWithGoogle;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Google;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.CompleteGoogleAccountLink;

[Collection(IntegrationTestCollection.Name)]
public sealed class CompleteGoogleAccountLinkTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Email = "google-link@example.com";
    private const string Password = "StrongPassword123!";
    private const string Endpoint = "/api/auth/google/link";
    private const string SignInEndpoint = "/api/auth/google/sign-in";
    private const string Subject = "google-subject";
    private static readonly string Token = new('A', 64);

    [Fact]
    public async Task Link_ExistingAccount_SupportsTheNextGoogleSignIn()
    {
        var accountId = await RegisterAsync(Email);
        var crmSession = await LoginAsync(Email, Password);
        using var clockFactory = Fixture.WithTimeProvider(TimeProvider.System);
        using var googleFactory = clockFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Replace(ServiceDescriptor.Singleton<IGoogleSignInTokenVerifier>(new StubGoogleVerifier()))));
        using var googleClient = googleFactory.CreateClient();

        var continuation = await SignInWithGoogleAsync(googleClient);
        Assert.Equal("linkRequired", continuation.Status);
        Assert.NotNull(continuation.ContinuationToken);

        using var response = await LinkAsync(crmSession.Login.AccessToken, continuation.ContinuationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(Subject, (await ExecuteDbAsync(db => db.GoogleSignInAccountLinks.SingleAsync())).GoogleSubject);
        Assert.False(await ExecuteDbAsync(db => db.GoogleSignInContinuations.AnyAsync()));

        using var replay = await LinkAsync(crmSession.Login.AccessToken, continuation.ContinuationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        await AssertProblemCodeAsync(replay, "Auth.InvalidGoogleLinkContinuation");

        var googleSession = await SignInWithGoogleAsync(googleClient);
        Assert.Equal("signedIn", googleSession.Status);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(googleSession.AccessToken);
        Assert.Equal(accountId.Value.ToString(), jwt.Subject);
    }

    [Fact]
    public async Task Link_AnonymousOrDifferentCrmAccount_CannotUseContinuation()
    {
        var owner = await RegisterAsync(Email);
        await RegisterAsync("other-link@example.com");
        var otherSession = await LoginAsync("other-link@example.com", Password);
        await SeedContinuationAsync(owner);

        using var anonymous = await Client.PostAsJsonAsync(Endpoint, new CompleteGoogleAccountLinkRequest(Token));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        using var otherAccount = await LinkAsync(otherSession.Login.AccessToken, Token);
        Assert.Equal(HttpStatusCode.Unauthorized, otherAccount.StatusCode);
        await AssertProblemCodeAsync(otherAccount, "Auth.InvalidGoogleLinkContinuation");
        Assert.False(await ExecuteDbAsync(db => db.GoogleSignInAccountLinks.AnyAsync()));
        Assert.Equal(1, await ExecuteDbAsync(db => db.GoogleSignInContinuations.CountAsync()));
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("registration")]
    [InlineData("unknown")]
    public async Task Link_InvalidContinuation_CannotCreateLink(string kind)
    {
        var owner = await RegisterAsync(Email);
        var session = await LoginAsync(Email, Password);
        if (kind != "unknown")
            await SeedContinuationAsync(kind == "registration" ? null : owner, expired: kind == "expired");

        using var response = await LinkAsync(session.Login.AccessToken, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemCodeAsync(response, "Auth.InvalidGoogleLinkContinuation");
        Assert.False(await ExecuteDbAsync(db => db.GoogleSignInAccountLinks.AnyAsync()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Link_SubjectOrCrmAccountAlreadyLinked_PreservesExistingLink(bool subjectAlreadyLinked)
    {
        var owner = await RegisterAsync(Email);
        var linkedAccount = subjectAlreadyLinked ? await RegisterAsync("previous-link@example.com") : owner;
        var linkedSubject = subjectAlreadyLinked ? Subject : "previous-google-subject";
        var session = await LoginAsync(Email, Password);
        await SeedContinuationAsync(owner);
        await ExecuteDbAsync(async db =>
        {
            db.GoogleSignInAccountLinks.Add(new GoogleSignInAccountLink(linkedSubject, linkedAccount));
            await db.SaveChangesAsync();
        });

        using var response = await LinkAsync(session.Login.AccessToken, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemCodeAsync(response, "Auth.GoogleAccountLinkConflict");
        var existing = await ExecuteDbAsync(db => db.GoogleSignInAccountLinks.SingleAsync());
        Assert.Equal(linkedAccount, existing.AccountId);
        Assert.Equal(linkedSubject, existing.GoogleSubject);
        Assert.Equal(1, await ExecuteDbAsync(db => db.GoogleSignInContinuations.CountAsync()));
    }

    [Fact]
    public async Task Link_ConcurrentRequests_ConsumeContinuationOnce()
    {
        var owner = await RegisterAsync(Email);
        var session = await LoginAsync(Email, Password);
        await SeedContinuationAsync(owner);

        var responses = await Task.WhenAll(
            LinkAsync(session.Login.AccessToken, Token),
            LinkAsync(session.Login.AccessToken, Token));
        using var linked = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.NoContent);
        using var rejected = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);

        await AssertProblemCodeAsync(rejected, "Auth.InvalidGoogleLinkContinuation");
        Assert.Equal(1, await ExecuteDbAsync(db => db.GoogleSignInAccountLinks.CountAsync()));
        Assert.False(await ExecuteDbAsync(db => db.GoogleSignInContinuations.AnyAsync()));
    }

    private async Task<UserAccountId> RegisterAsync(string email)
    {
        await RegisterAndConfirmStudentAsync(email, Password);
        return await ExecuteDbAsync(db => db.UserAccounts
            .Where(account => account.Email.Value == email).Select(account => account.Id).SingleAsync());
    }

    private Task SeedContinuationAsync(UserAccountId? accountId, bool expired = false) => ExecuteDbAsync(async db =>
    {
        db.GoogleSignInContinuations.Add(new GoogleSignInContinuation(
            GoogleSignInContinuation.HashToken(Token), Subject, Email,
            accountId, DateTimeOffset.UtcNow.AddMinutes(expired ? -1 : 5)));
        await db.SaveChangesAsync();
    });

    private async Task<HttpResponseMessage> LinkAsync(string accessToken, string continuationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, Endpoint, accessToken,
            new CompleteGoogleAccountLinkRequest(continuationToken));
        return await Client.SendAsync(request);
    }

    private static async Task<SignInWithGoogleResponse> SignInWithGoogleAsync(HttpClient client)
    {
        using var challenge = await client.PostAsync("/api/auth/google/challenge", content: null);
        challenge.EnsureSuccessStatusCode();
        var result = Assert.IsType<CreateGoogleSignInChallengeResponse>(
            await challenge.Content.ReadFromJsonAsync<CreateGoogleSignInChallengeResponse>());

        using var signIn = await client.PostAsJsonAsync(SignInEndpoint, new SignInWithGoogleRequest(result.Nonce));
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        return Assert.IsType<SignInWithGoogleResponse>(
            await signIn.Content.ReadFromJsonAsync<SignInWithGoogleResponse>());
    }

    private sealed class StubGoogleVerifier : IGoogleSignInTokenVerifier
    {
        public Task<VerifiedGoogleSignInIdentity> VerifyAsync(string idToken, CancellationToken cancellationToken) =>
            Task.FromResult(new VerifiedGoogleSignInIdentity(Subject, idToken, Email));
    }
}
