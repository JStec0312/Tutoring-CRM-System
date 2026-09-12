using System.Net;
using Microsoft.EntityFrameworkCore;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.SignOutAll;

public sealed class SignOutAllTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task SignOutAll_WithAuthenticatedUser_ShouldReturnNoContent()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterAndConfirmStudentAsync(email, password);

        var login = await LoginAsync(email, password);

        var response = await SendSignOutAllAsync(login.Login.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(HasRefreshTokenDeletionCookie(response));
    }

    [Fact]
    public async Task SignOutAll_ShouldRevokeAllUserRefreshTokens()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterAndConfirmStudentAsync(email, password);

        await LoginAsync(email, password);
        await LoginAsync(email, password);

        var login = await LoginAsync(email, password);

        var response = await SendSignOutAllAsync(login.Login.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var activeTokens = await GetActiveTokenCountAsync(email);

        Assert.Equal(0, activeTokens);
    }

    [Fact]
    public async Task SignOutAll_ShouldNotRevokeOtherUsersRefreshTokens()
    {
        const string firstEmail = "student-a@test.pl";
        const string secondEmail = "student-b@test.pl";
        const string password = "Password123!";

        await RegisterAndConfirmStudentAsync(firstEmail, password);
        await RegisterAndConfirmStudentAsync(secondEmail, password);

        var firstLogin = await LoginAsync(firstEmail, password);
        await LoginAsync(secondEmail, password);

        var response = await SendSignOutAllAsync(firstLogin.Login.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var otherUserHasActiveToken = await GetActiveTokenCountAsync(secondEmail);

        Assert.Equal(1, otherUserHasActiveToken);
    }

    [Fact]
    public async Task SignOutAll_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var response = await Client.PostAsync("/api/auth/sign-out-all", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendSignOutAllAsync(string accessToken)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/auth/sign-out-all",
            accessToken);

        return await Client.SendAsync(request);
    }

    private async Task<int> GetActiveTokenCountAsync(string email)
    {
        var userAccountId = await ExecuteDbAsync(dbContext =>
            dbContext.UserAccounts
                .Where(userAccount => userAccount.Email.Value == email)
                .Select(userAccount => userAccount.Id)
                .SingleAsync());

        return await ExecuteDbAsync(dbContext =>
            dbContext.RefreshTokens
                .CountAsync(refreshToken =>
                    refreshToken.UserAccountId == userAccountId &&
                    refreshToken.RevokedAtUtc == null));
    }

}
