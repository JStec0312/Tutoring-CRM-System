using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.Mobile;
using Tutoring.Domain.Identity;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.Mobile;

[Collection(IntegrationTestCollection.Name)]
public sealed class MobileSessionTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Email = "mobile-session@test.pl";
    private const string Password = "Password123!";
    private static string Path(string route) => $"/{route}";

    private async Task<MobileSessionResponse> LoginMobileAsync()
    {
        var response = await Client.PostAsJsonAsync(Path(MobileAuthRoutes.Login), new { Email, Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.True(response.Headers.CacheControl?.NoStore);
        return (await response.Content.ReadFromJsonAsync<MobileSessionResponse>())!;
    }

    [Fact]
    public async Task Login_ReturnsBothTokensWithoutCookie_StoresOnlyRefreshHash()
    {
        await RegisterAndConfirmStudentAsync(Email, Password);
        var session = await LoginMobileAsync();
        Assert.False(string.IsNullOrWhiteSpace(session.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(session.RefreshToken));
        Assert.True(session.ExpiresAt > DateTime.UtcNow);
        Assert.True(session.RefreshTokenExpiresAt > session.ExpiresAt);
        var stored = await ExecuteDbAsync(db => db.RefreshTokens.SingleAsync());
        Assert.NotEqual(session.RefreshToken, stored.TokenHash);
        Assert.False(stored.IsRevoked);
    }

    [Fact]
    public async Task Refresh_RotatesBodyToken_RejectsReuseAndRevokesFamily()
    {
        await RegisterAndConfirmStudentAsync(Email, Password);
        var first = await LoginMobileAsync();
        var response = await Client.PostAsJsonAsync(Path(MobileAuthRoutes.Refresh), new { first.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        var second = (await response.Content.ReadFromJsonAsync<MobileSessionResponse>())!;
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);

        var reuse = await Client.PostAsJsonAsync(Path(MobileAuthRoutes.Refresh), new { first.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        var problem = await reuse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Auth.InvalidRefreshToken", problem.GetProperty("code").GetString());
        var tokens = await ExecuteDbAsync(db => db.RefreshTokens.ToListAsync());
        Assert.Equal(2, tokens.Count);
        Assert.All(tokens, token => Assert.True(token.IsRevoked));
        var revoked = await Client.PostAsJsonAsync(Path(MobileAuthRoutes.Refresh), new { second.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
    }

    [Fact]
    public async Task SignOut_RevokesOnlyCurrentSession_IsIdempotent()
    {
        await RegisterAndConfirmStudentAsync(Email, Password);
        var current = await LoginMobileAsync();
        var other = await LoginMobileAsync();
        for (var i = 0; i < 2; i++)
        {
            var response = await Client.PostAsJsonAsync(Path(MobileAuthRoutes.SignOut), new { current.RefreshToken });
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
        var denied = await Client.PostAsJsonAsync(Path(MobileAuthRoutes.Refresh), new { current.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        var allowed = await Client.PostAsJsonAsync(Path(MobileAuthRoutes.Refresh), new { other.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown-token")]
    public async Task Refresh_InvalidToken_Returns401WithoutWrites(string refreshToken)
    {
        var response = await Client.PostAsJsonAsync(Path(MobileAuthRoutes.Refresh), new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, await ExecuteDbAsync(db => db.RefreshTokens.CountAsync()));
    }

    [Fact]
    public async Task Refresh_MissingBodyToken_DoesNotFallBackToCookie()
    {
        await RegisterAndConfirmStudentAsync(Email, Password);
        var session = await LoginAsync(Email, Password);
        using var request = new HttpRequestMessage(HttpMethod.Post, Path(MobileAuthRoutes.Refresh))
        {
            Content = JsonContent.Create(new { })
        };
        request.Headers.Add("Cookie", $"{RefreshTokenCookieName}={session.RefreshToken}");
        var response = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False((await ExecuteDbAsync(db => db.RefreshTokens.SingleAsync())).IsRevoked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Refresh_ExpiredTokenOrSuspendedAccount_Returns401(bool expireToken)
    {
        await RegisterAndConfirmStudentAsync(Email, Password);
        var session = await LoginMobileAsync();
        await ExecuteDbAsync(async db =>
        {
            if (expireToken)
            {
                var token = await db.RefreshTokens.SingleAsync();
                db.Entry(token).Property(item => item.ExpiresAtUtc).CurrentValue = DateTime.UtcNow.AddMinutes(-1);
            }
            else
            {
                var user = await db.UserAccounts.SingleAsync();
                db.Entry(user).Property(item => item.Status).CurrentValue = AccountStatus.Suspended;
            }
            await db.SaveChangesAsync();
        });
        var response = await Client.PostAsJsonAsync(Path(MobileAuthRoutes.Refresh), new { session.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidPassword_DoesNotCreateSession()
    {
        await RegisterAndConfirmStudentAsync(Email, Password);
        var response = await Client.PostAsJsonAsync(Path(MobileAuthRoutes.Login), new { Email, Password = "WrongPassword!" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, await ExecuteDbAsync(db => db.RefreshTokens.CountAsync()));
    }

    [Fact]
    public async Task WebLogin_StillKeepsRefreshTokenOutOfJson()
    {
        await RegisterAndConfirmStudentAsync(Email, Password);
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { Email, Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Set-Cookie"));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.TryGetProperty("refreshToken", out _));
    }
}
