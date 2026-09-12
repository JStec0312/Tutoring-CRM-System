using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Users.UpdateProfile;

public sealed class UpdateProfileTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task UpdateProfile_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await Client.PutAsJsonAsync(
            "/api/users/me/profile",
            new
            {
                UserName = "updated-user",
                FirstName = "Updated",
                LastName = "User",
                PhoneNumber = "123456789"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_ShouldReturnNoContentAndPersistProfile()
    {
        const string email = "student@test.pl";
        const string password = "Password123!";

        await RegisterAndConfirmStudentAsync(email, password);
        var accessToken = (await LoginAsync(email, password)).Login.AccessToken;

        var response = await SendUpdateProfileAsync(
            accessToken,
            new
            {
                UserName = "updated-user",
                FirstName = "Updated",
                LastName = "User",
                PhoneNumber = "123456789"
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var account = await ExecuteDbAsync(dbContext =>
            dbContext.UserAccounts
                .AsNoTracking()
                .SingleAsync(account => account.Email.Value == email));

        Assert.Equal("updated-user", account.Profile.UserName);
        Assert.Equal("Updated", account.Profile.FirstName);
        Assert.Equal("User", account.Profile.LastName);
        Assert.Equal("123456789", account.Profile.PhoneNumber!.Value);
    }

    [Fact]
    public async Task UpdateProfile_WithExistingUserName_ShouldReturnConflict()
    {
        const string password = "Password123!";
        await RegisterAndConfirmStudentAsync("first@test.pl", password);
        await RegisterAndConfirmStudentAsync("second@test.pl", password);

        var accessToken = (await LoginAsync("second@test.pl", password)).Login.AccessToken;

        var response = await SendUpdateProfileAsync(
            accessToken,
            new
            {
                UserName = "first",
                FirstName = "Second",
                LastName = "User",
                PhoneNumber = (string?)null
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithExistingPhoneNumber_ShouldReturnConflict()
    {
        const string password = "Password123!";
        await RegisterAndConfirmStudentAsync("first@test.pl", password);
        await RegisterAndConfirmStudentAsync("second@test.pl", password);

        var firstAccessToken = (await LoginAsync("first@test.pl", password)).Login.AccessToken;
        var secondAccessToken = (await LoginAsync("second@test.pl", password)).Login.AccessToken;

        var firstUpdateResponse = await SendUpdateProfileAsync(
            firstAccessToken,
            new
            {
                UserName = "first",
                FirstName = "First",
                LastName = "User",
                PhoneNumber = "123456789"
            });
        Assert.Equal(HttpStatusCode.NoContent, firstUpdateResponse.StatusCode);

        var response = await SendUpdateProfileAsync(
            secondAccessToken,
            new
            {
                UserName = "second",
                FirstName = "Second",
                LastName = "User",
                PhoneNumber = "123456789"
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendUpdateProfileAsync(
        string accessToken,
        object request)
    {
        using var httpRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            "/api/users/me/profile",
            accessToken,
            request);

        return await Client.SendAsync(httpRequest);
    }
}
