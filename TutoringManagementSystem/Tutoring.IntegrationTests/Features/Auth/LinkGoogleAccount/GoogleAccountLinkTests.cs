using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.CreateGoogleChallenge;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Google;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.LinkGoogleAccount;

[Collection(IntegrationTestCollection.Name)]
public sealed class GoogleAccountLinkTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    private const string ChallengePath = "/api/users/me/google/challenge";
    private const string LinkPath = "/api/users/me/google";
    private const string Password = "Password123!";
    private const string Email = "google-link@test.pl";
    private const string OtherEmail = "other-google-link@test.pl";
    private const string GoogleSubject = "110123456789012345678";

    [Fact]
    public async Task Challenge_ReturnsDistinctNoncesBoundToAuthenticatedAccount()
    {
        var accessToken = await RegisterAndLoginAsync(Email);
        var now = DateTimeOffset.UtcNow;
        using var factory = Fixture.WithTimeProvider(new FixedTimeProvider(now));
        using var client = factory.CreateClient();

        var first = await CreateChallengeAsync(client, accessToken);
        var second = await CreateChallengeAsync(client, accessToken);

        Assert.Matches("^[0-9A-F]{64}$", first);
        Assert.NotEqual(first, second);
        var account = await ExecuteDbAsync(db => db.UserAccounts.SingleAsync());
        var challenges = await ExecuteDbAsync(db => db.GoogleSignInChallenges.ToListAsync());
        Assert.Equal(2, challenges.Count);
        Assert.All(challenges, challenge =>
        {
            Assert.Equal(account.Id.Value, challenge.AccountId);
            Assert.Equal(now.AddMinutes(5), challenge.ExpiresAtUtc);
        });
    }

    [Theory]
    [InlineData(ChallengePath)]
    [InlineData(LinkPath)]
    public async Task GoogleEndpoints_RequireCrmAccessToken(string path)
    {
        using var response = await Client.PostAsJsonAsync(path,
            new { IdToken = "token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(await ExecuteDbAsync(db => db.GoogleSignInChallenges.AnyAsync()));
        await AssertNoLinkAsync();
    }

    [Fact]
    public async Task Link_ConcurrentRequestsWithSameChallenge_CreateOneLink()
    {
        var accessToken = await RegisterAndLoginAsync(Email);
        var nonce = await CreateChallengeAsync(Client, accessToken);
        var verifier = new TestGoogleVerifier();
        var idToken = verifier.AddToken(GoogleSubject, nonce);
        using var factory = Fixture.WithGoogleIdTokenVerifier(verifier);
        using var client = factory.CreateClient();

        var responses = await Task.WhenAll(
            LinkAsync(client, accessToken, idToken),
            LinkAsync(client, accessToken, idToken));
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.NoContent);
            var rejected = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.BadRequest);
            await AssertProblemAsync(rejected, HttpStatusCode.BadRequest, "Auth.InvalidGoogleCredential");
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        var account = await ExecuteDbAsync(db => db.UserAccounts.SingleAsync());
        var link = await ExecuteDbAsync(db => db.GoogleAccountLinks.SingleAsync());
        Assert.Equal(account.Id, link.AccountId);
        Assert.Equal(GoogleSubject, link.GoogleSubject);
        Assert.False(await ExecuteDbAsync(db => db.GoogleSignInChallenges.AnyAsync()));
    }

    [Fact]
    public async Task Link_RejectsAnotherAccountsChallengeAndPreservesItForOwner()
    {
        var owner = await RegisterAndLoginAsync(Email);
        var other = await RegisterAndLoginAsync(OtherEmail);
        var nonce = await CreateChallengeAsync(Client, owner);
        var verifier = new TestGoogleVerifier();
        var idToken = verifier.AddToken(GoogleSubject, nonce);
        using var factory = Fixture.WithGoogleIdTokenVerifier(verifier);
        using var client = factory.CreateClient();

        using var rejected = await LinkAsync(client, other, idToken);
        await AssertProblemAsync(rejected, HttpStatusCode.BadRequest, "Auth.InvalidGoogleCredential");
        await AssertNoLinkAsync();

        using var allowed = await LinkAsync(client, owner, idToken);
        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
        var ownerAccount = await ExecuteDbAsync(db => db.UserAccounts.SingleAsync(user => user.Email.Value == Email));
        Assert.Equal(ownerAccount.Id, (await ExecuteDbAsync(db => db.GoogleAccountLinks.SingleAsync())).AccountId);
    }

    [Fact]
    public async Task Link_RejectsExpiredChallenge()
    {
        var accessToken = await RegisterAndLoginAsync(Email);
        var nonce = await CreateChallengeAsync(Client, accessToken);
        var expiredAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await ExecuteDbAsync(db => db.GoogleSignInChallenges.Where(challenge => challenge.Nonce == nonce)
            .ExecuteUpdateAsync(setters => setters.SetProperty(challenge => challenge.ExpiresAtUtc, expiredAt)));

        var verifier = new TestGoogleVerifier();
        var idToken = verifier.AddToken(GoogleSubject, nonce);
        using var factory = Fixture.WithGoogleIdTokenVerifier(verifier);
        using var client = factory.CreateClient();

        using var response = await LinkAsync(client, accessToken, idToken);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Auth.InvalidGoogleCredential");
        await AssertNoLinkAsync();
    }

    [Fact]
    public async Task Link_InvalidGoogleToken_DoesNotConsumeChallenge()
    {
        var accessToken = await RegisterAndLoginAsync(Email);
        var nonce = await CreateChallengeAsync(Client, accessToken);
        using var factory = Fixture.WithGoogleIdTokenVerifier(new TestGoogleVerifier());
        using var client = factory.CreateClient();

        using var response = await LinkAsync(client, accessToken, "invalid-google-id-token");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Auth.InvalidGoogleCredential");
        Assert.True(await ExecuteDbAsync(db => db.GoogleSignInChallenges.AnyAsync(challenge => challenge.Nonce == nonce)));
        await AssertNoLinkAsync();
    }

    [Fact]
    public async Task Link_MissingIdToken_DoesNotConsumeChallenge()
    {
        var accessToken = await RegisterAndLoginAsync(Email);
        var nonce = await CreateChallengeAsync(Client, accessToken);

        using var request = CreateAuthorizedRequest(HttpMethod.Post, LinkPath, accessToken, new { });
        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(await ExecuteDbAsync(db => db.GoogleSignInChallenges.AnyAsync(challenge => challenge.Nonce == nonce)));
        await AssertNoLinkAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Link_ExistingCrmOrGoogleAssociation_ReturnsConflict(bool sameCrmAccount)
    {
        var firstAccessToken = await RegisterAndLoginAsync(Email);
        var firstNonce = await CreateChallengeAsync(Client, firstAccessToken);
        var secondAccessToken = sameCrmAccount ? firstAccessToken : await RegisterAndLoginAsync(OtherEmail);
        var secondNonce = await CreateChallengeAsync(Client, secondAccessToken);

        var verifier = new TestGoogleVerifier();
        var firstIdToken = verifier.AddToken(GoogleSubject, firstNonce);
        var secondIdToken = verifier.AddToken(sameCrmAccount ? "110987654321098765432" : GoogleSubject, secondNonce);
        using var factory = Fixture.WithGoogleIdTokenVerifier(verifier);
        using var client = factory.CreateClient();

        using var first = await LinkAsync(client, firstAccessToken, firstIdToken);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        using var second = await LinkAsync(client, secondAccessToken, secondIdToken);
        await AssertProblemAsync(second, HttpStatusCode.Conflict, "Auth.GoogleLinkConflict");

        var firstAccount = await ExecuteDbAsync(db => db.UserAccounts.SingleAsync(user => user.Email.Value == Email));
        var link = await ExecuteDbAsync(db => db.GoogleAccountLinks.SingleAsync());
        Assert.Equal(firstAccount.Id, link.AccountId);
        Assert.Equal(GoogleSubject, link.GoogleSubject);
    }

    [Fact]
    public async Task GoogleEndpoints_RejectInactiveAccountWithExistingJwt()
    {
        var accessToken = await RegisterAndLoginAsync(Email);
        var nonce = await CreateChallengeAsync(Client, accessToken);
        await ExecuteDbAsync(db => db.UserAccounts
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.Status, AccountStatus.Suspended)));

        var verifier = new TestGoogleVerifier();
        var idToken = verifier.AddToken(GoogleSubject, nonce);
        using var factory = Fixture.WithGoogleIdTokenVerifier(verifier);
        using var client = factory.CreateClient();

        using var challenge = CreateAuthorizedRequest(HttpMethod.Post, ChallengePath, accessToken);
        using var challengeResponse = await client.SendAsync(challenge);
        using var linkResponse = await LinkAsync(client, accessToken, idToken);

        await AssertProblemAsync(challengeResponse, HttpStatusCode.Forbidden, "Auth.InactiveAccount.");
        await AssertProblemAsync(linkResponse, HttpStatusCode.Forbidden, "Auth.InactiveAccount.");
        await AssertNoLinkAsync();
    }

    private async Task<string> RegisterAndLoginAsync(string email)
    {
        await RegisterAndConfirmStudentAsync(email, Password);
        return (await LoginAsync(email, Password)).Login.AccessToken;
    }

    private async Task<string> CreateChallengeAsync(HttpClient client, string accessToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, ChallengePath, accessToken);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        return (await response.Content.ReadFromJsonAsync<CreateGoogleChallengeResponse>())!.Nonce;
    }

    private async Task<HttpResponseMessage> LinkAsync(
        HttpClient client, string accessToken, string idToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, LinkPath, accessToken,
            new { IdToken = idToken });
        return await client.SendAsync(request);
    }

    private async Task AssertNoLinkAsync() =>
        Assert.False(await ExecuteDbAsync(db => db.GoogleAccountLinks.AnyAsync()));

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, problem.GetProperty("code").GetString());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class TestGoogleVerifier : IGoogleIdTokenVerifier
    {
        private readonly Dictionary<string, VerifiedGoogleIdentity> _tokens = new();

        public string AddToken(string subject, string nonce)
        {
            var token = Guid.NewGuid().ToString("N");
            _tokens.Add(token, new VerifiedGoogleIdentity(subject, nonce));
            return token;
        }

        public Task<VerifiedGoogleIdentity> VerifyAsync(string idToken, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_tokens.TryGetValue(idToken, out var identity))
            {
                throw new InvalidJwtException("Invalid test credential.");
            }

            return Task.FromResult(identity);
        }
    }
}
