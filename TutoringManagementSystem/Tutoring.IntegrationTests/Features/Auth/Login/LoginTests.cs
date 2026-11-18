using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Authentication.Google;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.Login
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class LoginTests(
        IntegrationTestFixture fixture)
        : IntegrationTestBase(fixture)
    {
        [Theory]
        [InlineData("/api/auth/login", true, true)]
        [InlineData("/api/auth/mobile/login", true, true)]
        [InlineData("/api/auth/login", false, true)]
        [InlineData("/api/auth/mobile/login", false, true)]
        [InlineData("/api/auth/login", true, false)]
        public async Task Login_WithoutPassword_ShouldNameGoogleOnlyForActiveLinkedAccounts(
            string endpoint, bool active, bool linked)
        {
            const string email = "google-only@test.pl";
            await ExecuteDbAsync(async db =>
            {
                var account = new UserAccount(new EmailAddress(email), null,
                    new PersonalProfile("google-only", null, null, null), DateTimeOffset.UtcNow);
                if (active)
                    account.Activate();
                db.UserAccounts.Add(account);
                if (linked)
                    db.GoogleSignInAccountLinks.Add(new GoogleSignInAccountLink("google-subject", account.Id));
                await db.SaveChangesAsync();
            });

            using var response = await Client.PostAsJsonAsync(endpoint, new { Email = email, Password = "AnyPassword123!" });

            if (active && linked)
            {
                Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
                await AssertProblemCodeAsync(response, "Auth.GoogleAccountHasNoPassword");
            }
            else
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                await AssertProblemCodeAsync(response, "Auth.InvalidCredentials");
            }
            Assert.False(response.Headers.Contains("Set-Cookie"));
            Assert.False(await ExecuteDbAsync(db => db.RefreshTokens.AnyAsync()));
        }

        [Theory]
        [InlineData("/api/auth/login", "Password123!", HttpStatusCode.OK)]
        [InlineData("/api/auth/mobile/login", "Password123!", HttpStatusCode.OK)]
        [InlineData("/api/auth/login", "WrongPassword123!", HttpStatusCode.Unauthorized)]
        [InlineData("/api/auth/mobile/login", "WrongPassword123!", HttpStatusCode.Unauthorized)]
        public async Task Login_GoogleLinkedAccountWithPassword_ShouldUseNormalPasswordValidation(
            string endpoint, string password, HttpStatusCode expectedStatus)
        {
            const string email = "linked-with-password@test.pl";
            await RegisterAndConfirmStudentAsync(email, "Password123!");
            await ExecuteDbAsync(async db =>
            {
                var account = await db.UserAccounts.SingleAsync(user => user.Email.Value == email);
                db.GoogleSignInAccountLinks.Add(new GoogleSignInAccountLink("linked-subject", account.Id));
                await db.SaveChangesAsync();
            });

            using var response = await Client.PostAsJsonAsync(endpoint, new { Email = email, Password = password });

            Assert.Equal(expectedStatus, response.StatusCode);
            if (expectedStatus == HttpStatusCode.Unauthorized)
            {
                await AssertProblemCodeAsync(response, "Auth.InvalidCredentials");
                Assert.False(await ExecuteDbAsync(db => db.RefreshTokens.AnyAsync()));
            }
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnOkWithAccessToken()
        {
            const string email = "student@test.pl";
            const string password = "Password123!";

            await RegisterAndConfirmStudentAsync(email, password);

            var session = await LoginAsync(email, password);

            Assert.False(string.IsNullOrWhiteSpace(session.Login.AccessToken));
        }

        [Fact]
        public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
        {
            var response = await Client.PostAsJsonAsync(
                "/api/auth/login",
                new { Email = "unknown@test.pl", Password = "Password123!" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
        {
            const string email = "student@test.pl";

            await RegisterAndConfirmStudentAsync(email, "Password123!");

            var response = await Client.PostAsJsonAsync(
                "/api/auth/login",
                new { Email = email, Password = "WrongPassword123!" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
