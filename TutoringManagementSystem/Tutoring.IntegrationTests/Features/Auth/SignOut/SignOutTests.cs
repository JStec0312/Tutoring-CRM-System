using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tutoring.Api.Features.Auth.Login;
using Tutoring.Infrastructure.Authentication;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.SignOut;

public sealed class SignOutTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private string RefreshTokenCookieName =>
        Fixture.Services.GetRequiredService<IOptions<RefreshTokenOptions>>().Value.RefreshTokenCookieName;

    [Fact]
    public async Task SignOut_WithValidRefreshToken_ShouldReturnNoContent()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterStudentAsync(email, password);

        var refreshToken = await LoginAsync(email, password);

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

        await RegisterStudentAsync(email, password);

        var refreshToken = await LoginAsync(email, password);

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

    private async Task RegisterStudentAsync(
        string email,
        string password)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/student",
            new
            {
                Email = email,
                Username = "student1",
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

    private async Task<HttpResponseMessage> SendSignOutAsync(string? refreshToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/auth/sign-out");

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
