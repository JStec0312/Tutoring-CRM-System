using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.CreateGoogleSignInChallenge;
using Tutoring.Infrastructure.Authentication.Google;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.CreateGoogleSignInChallenge;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreateGoogleSignInChallengeTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Endpoint = "/api/auth/google/challenge";
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_WithoutAuthenticationOrBody_ReturnsAndPersistsFreshNonces()
    {
        using var factory = Fixture.WithTimeProvider(new FixedTimeProvider());
        using var client = factory.CreateClient();
        using var firstResponse = await client.PostAsync(Endpoint, content: null);
        using var secondResponse = await client.PostAsJsonAsync(Endpoint, new CreateGoogleSignInChallengeRequest());

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.True(firstResponse.Headers.CacheControl?.NoStore);

        var first = Assert.IsType<CreateGoogleSignInChallengeResponse>(
            await firstResponse.Content.ReadFromJsonAsync<CreateGoogleSignInChallengeResponse>());
        var second = Assert.IsType<CreateGoogleSignInChallengeResponse>(
            await secondResponse.Content.ReadFromJsonAsync<CreateGoogleSignInChallengeResponse>());

        Assert.Equal(32, Convert.FromHexString(first.Nonce).Length);
        Assert.NotEqual(first.Nonce, second.Nonce);
        Assert.Equal(Now.AddMinutes(5), first.ExpiresAtUtc);
        Assert.Equal(first.ExpiresAtUtc, second.ExpiresAtUtc);
        var persisted = await ExecuteDbAsync(db => db.GoogleAuthenticationChallenges.AsNoTracking().ToListAsync());
        Assert.Equal(2, persisted.Count);
        Assert.Contains(persisted, row => row.Nonce == first.Nonce && row.ExpiresAtUtc == first.ExpiresAtUtc);
        Assert.Contains(persisted, row => row.Nonce == second.Nonce && row.ExpiresAtUtc == second.ExpiresAtUtc);
    }

    [Fact]
    public async Task Create_RemovesExpiredChallengesAndRetainsValidChallenges()
    {
        var expiredNonce = new string('A', 64);
        var validNonce = new string('B', 64);
        await ExecuteDbAsync(async db =>
        {
            db.GoogleAuthenticationChallenges.AddRange(
                new GoogleAuthenticationChallenge(expiredNonce, Now),
                new GoogleAuthenticationChallenge(validNonce, Now.AddSeconds(1)));
            await db.SaveChangesAsync();
        });

        using var factory = Fixture.WithTimeProvider(new FixedTimeProvider());
        using var client = factory.CreateClient();
        using var response = await client.PostAsync(Endpoint, content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var nonces = await ExecuteDbAsync(db => db.GoogleAuthenticationChallenges.Select(row => row.Nonce).ToListAsync());
        Assert.Equal(2, nonces.Count);
        Assert.Contains(validNonce, nonces);
        Assert.DoesNotContain(expiredNonce, nonces);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
