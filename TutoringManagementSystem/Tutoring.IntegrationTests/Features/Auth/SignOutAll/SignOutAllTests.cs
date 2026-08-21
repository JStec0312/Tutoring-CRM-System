using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tutoring.Api.Features.Auth.Login;
using Tutoring.Infrastructure.Authentication;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.SignOutAll;

public sealed class SignOutAllTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private string RefreshTokenCookieName =>
        Fixture.Services.GetRequiredService<IOptions<RefreshTokenOptions>>().Value.RefreshTokenCookieName;

    [Fact]
    public async Task SignOutAll_WithAuthenticatedUser_ShouldReturnNoContent()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterStudentAsync(email, password);

        var login = await LoginAsync(email, password);

        var response = await SendSignOutAllAsync(login.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(HasRefreshTokenDeletionCookie(response));
    }

    [Fact]
    public async Task SignOutAll_ShouldRevokeAllUserRefreshTokens()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterStudentAsync(email, password);

        await LoginAsync(email, password);
        await LoginAsync(email, password);

        var login = await LoginAsync(email, password);

        var response = await SendSignOutAllAsync(login.AccessToken);

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

        await RegisterStudentAsync(firstEmail, password);
        await RegisterStudentAsync(secondEmail, password);

        var firstLogin = await LoginAsync(firstEmail, password);
        await LoginAsync(secondEmail, password);

        var response = await SendSignOutAllAsync(firstLogin.AccessToken);

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

    private async Task<LoginResponse> LoginAsync(
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

        return login!;
    }

    private async Task<HttpResponseMessage> SendSignOutAllAsync(string accessToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/auth/sign-out-all");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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

    private bool HasRefreshTokenDeletionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return false;
        }

        var options = Fixture.Services.GetRequiredService<IOptions<RefreshTokenOptions>>().Value;
        var sameSite = options.SameSiteRefreshTokenCookie.ToLowerInvariant();

        return values.Any(value =>
            value.Contains($"{RefreshTokenCookieName}=", StringComparison.OrdinalIgnoreCase) &&
            value.Contains($"path={options.RefreshTokenPath}", StringComparison.OrdinalIgnoreCase) &&
            value.Contains($"samesite={sameSite}", StringComparison.OrdinalIgnoreCase) &&
            (!options.SecureRefreshTokenCookie || value.Contains("secure", StringComparison.OrdinalIgnoreCase)));
    }
}
