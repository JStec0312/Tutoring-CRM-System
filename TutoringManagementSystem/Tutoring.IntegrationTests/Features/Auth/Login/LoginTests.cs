using System.Net;
using System.Net.Http.Json;
using Tutoring.Api.Features.Auth.Login;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth.Login
{
    public sealed class LoginTests(
        IntegrationTestFixture fixture)
        : IntegrationTestBase(fixture)
    {
        private async Task RegisterStudentAsync(
            string email,
            string password)
        {
            var request = new
            {
                Email = email,
                Username = "student1",
                Password = password
            };

            var response = await Client.PostAsJsonAsync(
                "/api/auth/register/student",
                request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnOkWithAccessToken()
        {
            const string email = "student@test.pl";
            const string password = "Password123!";

            await RegisterStudentAsync(email, password);

            var response = await Client.PostAsJsonAsync(
                "/api/auth/login",
                new { Email = email, Password = password });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

            Assert.NotNull(login);
            Assert.False(string.IsNullOrWhiteSpace(login!.AccessToken));
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

            await RegisterStudentAsync(email, "Password123!");

            var response = await Client.PostAsJsonAsync(
                "/api/auth/login",
                new { Email = email, Password = "WrongPassword123!" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
