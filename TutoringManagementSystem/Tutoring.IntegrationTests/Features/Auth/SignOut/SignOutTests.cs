using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.SignOut;

public sealed class SignOutTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task SignOut_WithValidRefreshToken_ShouldReturnNoContent()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterAndConfirmStudentAsync(email, password);

        var refreshToken = (await LoginAsync(email, password)).RefreshToken;

        var response = await SendSignOutAsync(refreshToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(HasRefreshTokenDeletionCookie(response));

        var revokedToken = await ExecuteDbAsync(dbContext =>
            dbContext.RefreshTokens.SingleAsync());

        Assert.NotNull(revokedToken.RevokedAtUtc);
    }

    [Fact]
    public async Task SignOut_ShouldDeleteRefreshCookie()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterAndConfirmStudentAsync(email, password);

        var refreshToken = (await LoginAsync(email, password)).RefreshToken;

        var response = await SendSignOutAsync(refreshToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(HasRefreshTokenDeletionCookie(response));
    }

    [Fact]
    public async Task SignOut_WithoutCookie_ShouldBeIdempotent()
    {
        var response = await SendSignOutAsync(null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(HasRefreshTokenDeletionCookie(response));
    }

    private async Task<HttpResponseMessage> SendSignOutAsync(string? refreshToken)
    {
        return await SendWithRefreshTokenAsync(
            HttpMethod.Post,
            "/api/auth/sign-out",
            refreshToken);
    }
}
