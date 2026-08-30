using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tutoring.Api.Features.Auth.Login;
using Tutoring.Api.Features.Auth.RefreshToken;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.RefreshToken;

public sealed class RefreshTokenTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private string RefreshTokenCookieName =>
        Fixture.Services.GetRequiredService<IOptions<RefreshTokenOptions>>().Value.RefreshTokenCookieName;

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewAccessToken()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterStudentAsync(email, password);
        var initialRefreshToken = await LoginAsync(email, password);

        var response = await SendRefreshAsync(initialRefreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Refresh_WithValidToken_RotatesRefreshToken()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterStudentAsync(email, password);
        var initialRefreshToken = await LoginAsync(email, password);

        var response = await SendRefreshAsync(initialRefreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var newRefreshToken = ExtractRefreshToken(response);
        Assert.False(string.IsNullOrWhiteSpace(newRefreshToken));
        Assert.NotEqual(initialRefreshToken, newRefreshToken);

        var nextRefreshResponse = await SendRefreshAsync(newRefreshToken);
        Assert.Equal(HttpStatusCode.OK, nextRefreshResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldRevokePreviousToken()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterStudentAsync(email, password);
        var initialRefreshToken = await LoginAsync(email, password);

        var response = await SendRefreshAsync(initialRefreshToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tokens = await ExecuteDbAsync(dbContext =>
            dbContext.RefreshTokens.ToListAsync());

        Assert.Equal(2, tokens.Count);

        var revokedToken = tokens.Single(t => t.IsRevoked);
        var activeToken = tokens.Single(t => !t.IsRevoked);

        Assert.NotNull(revokedToken.RevokedAtUtc);
        Assert.Equal(activeToken.Id, revokedToken.ReplacedByTokenId);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_ShouldReturnUnauthorized()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterStudentAsync(email, password);
        var initialRefreshToken = await LoginAsync(email, password);

        // First refresh revokes the initial token
        var firstResponse = await SendRefreshAsync(initialRefreshToken);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Attempting to use the already revoked initial token
        var secondResponse = await SendRefreshAsync(initialRefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ShouldReturnUnauthorized()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterStudentAsync(email, password);
        var initialRefreshToken = await LoginAsync(email, password);

        await ExecuteDbAsync(async dbContext =>
        {
            var token = await dbContext.RefreshTokens.SingleAsync();
            dbContext.Entry(token).Property(t => t.ExpiresAtUtc).CurrentValue = DateTime.UtcNow.AddMinutes(-10);
            await dbContext.SaveChangesAsync();
        });

        var response = await SendRefreshAsync(initialRefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ShouldReturnUnauthorized()
    {
        var response = await SendRefreshAsync("completely-unknown-token-value-12345");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithInactiveUser_ShouldReturnUnauthorized()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterStudentAsync(email, password);
        var initialRefreshToken = await LoginAsync(email, password);

        await ExecuteDbAsync(async dbContext =>
        {
            var user = await dbContext.UserAccounts.SingleAsync(u => u.Email.Value == email);
            dbContext.Entry(user).Property(u => u.Status).CurrentValue = AccountStatus.Suspended;
            await dbContext.SaveChangesAsync();
        });

        var response = await SendRefreshAsync(initialRefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithReusedToken_ShouldRevokeEntireFamily()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterStudentAsync(email, password);
        var token1 = await LoginAsync(email, password);

        // Rotate token1 -> token2
        var response1 = await SendRefreshAsync(token1);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var token2 = ExtractRefreshToken(response1);

        // Rotate token2 -> token3
        var response2 = await SendRefreshAsync(token2);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var token3 = ExtractRefreshToken(response2);

        // Verify token3 is initially active
        var tokensBeforeReuse = await ExecuteDbAsync(dbContext =>
            dbContext.RefreshTokens.ToListAsync());
        var activeTokenBeforeReuse = tokensBeforeReuse.Single(t => !t.IsRevoked);
        var familyId = activeTokenBeforeReuse.FamilyId;

        // Reuse revoked token1 (replay attack detection)
        var reuseResponse = await SendRefreshAsync(token1);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        // Verify all tokens in the family are now revoked
        var familyTokensAfterReuse = await ExecuteDbAsync(dbContext =>
            dbContext.RefreshTokens
                .Where(t => t.FamilyId == familyId)
                .ToListAsync());

        Assert.Equal(3, familyTokensAfterReuse.Count);
        Assert.All(familyTokensAfterReuse, t => Assert.NotNull(t.RevokedAtUtc));

        // Attempting to refresh with the previously active token3 should now also fail
        var token3Response = await SendRefreshAsync(token3);
        Assert.Equal(HttpStatusCode.Unauthorized, token3Response.StatusCode);
    }

    private async Task RegisterStudentAsync(
        string email,
        string password)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/student",
            new
            {
                Email = email,
                Username = email.Split('@')[0],
                Password = password
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<string> LoginAsync(
        string email,
        string password)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = email,
                Password = password
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);

        return ExtractRefreshToken(response);
    }

    private async Task<HttpResponseMessage> SendRefreshAsync(string? refreshToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/auth/refresh");

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            request.Headers.Add(
                "Cookie",
                $"{RefreshTokenCookieName}={refreshToken}");
        }

        return await Client.SendAsync(request);
    }

    private string ExtractRefreshToken(HttpResponseMessage response)
    {
        var setCookie = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(value =>
                value.StartsWith($"{RefreshTokenCookieName}=", StringComparison.OrdinalIgnoreCase))
            : null;

        Assert.NotNull(setCookie);

        var cookiePair = setCookie!.Split(';')[0];
        return cookiePair.Split('=', 2)[1];
    }
}
