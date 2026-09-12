using System.Net;
using System.Net.Http.Json;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.Login
{
    public sealed class LoginTests(
        IntegrationTestFixture fixture)
        : IntegrationTestBase(fixture)
    {
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
