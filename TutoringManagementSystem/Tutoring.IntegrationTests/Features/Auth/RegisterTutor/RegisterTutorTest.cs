using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.RegisterTutor;
using Tutoring.Domain.Identity;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth;

public sealed class RegisterTutorTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task RegisterTutor_WithValidData_ShouldCreateTutorAccount()
    {
        var request = new
        {
            Email = "tutor@test.pl",
            UserName = "tutor1",
            Password = "Password123!",
            FirstName = "Jane",
            LastName = "Tutor",
            PhoneNumber = "+48123456789"
        };

        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/tutor",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var registration = await response.Content.ReadFromJsonAsync<RegisterTutorResponse>();

        Assert.NotNull(registration);

        var tutor = await ExecuteDbAsync(dbContext =>
            dbContext.Tutors
                .Include(tutor => tutor.Account)
                .SingleAsync());

        Assert.Equal(registration.UserId, tutor.UserAccountId.Value);
        Assert.Equal(request.Email, tutor.Account.Email.Value);
        Assert.Equal(request.UserName, tutor.Account.Profile.UserName);
        Assert.Equal(request.FirstName, tutor.Account.Profile.FirstName);
        Assert.Equal(request.LastName, tutor.Account.Profile.LastName);
        Assert.Equal(request.PhoneNumber, tutor.Account.Profile.PhoneNumber!.Value);

        var roleAssignment = await ExecuteDbAsync(dbContext =>
            dbContext.UserAccountRoles.SingleAsync());

        Assert.Equal(tutor.UserAccountId, roleAssignment.UserAccountId);
        Assert.Equal(UserRole.Tutor, roleAssignment.Role);
    }

    [Fact]
    public async Task RegisterTutor_WithDuplicateEmail_ShouldReturnConflict()
    {
        var firstRequest = new
        {
            Email = "tutor@test.pl",
            UserName = "tutor1",
            Password = "Password123!"
        };
        var secondRequest = new
        {
            Email = "tutor@test.pl",
            UserName = "tutor2",
            Password = "Password123!"
        };

        await Client.PostAsJsonAsync("/api/auth/register/tutor", firstRequest);

        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/tutor",
            secondRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RegisterTutor_WithDuplicateUserName_ShouldReturnConflict()
    {
        var firstRequest = new
        {
            Email = "first-tutor@test.pl",
            UserName = "tutor1",
            Password = "Password123!"
        };
        var secondRequest = new
        {
            Email = "second-tutor@test.pl",
            UserName = "tutor1",
            Password = "Password123!"
        };

        await Client.PostAsJsonAsync("/api/auth/register/tutor", firstRequest);

        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/tutor",
            secondRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RegisterTutor_WithDuplicatePhoneNumber_ShouldReturnConflict()
    {
        var firstRequest = new
        {
            Email = "first-tutor@test.pl",
            UserName = "tutor1",
            Password = "Password123!",
            PhoneNumber = "+48123456789"
        };
        var secondRequest = new
        {
            Email = "second-tutor@test.pl",
            UserName = "tutor2",
            Password = "Password123!",
            PhoneNumber = "+48123456789"
        };

        await Client.PostAsJsonAsync("/api/auth/register/tutor", firstRequest);

        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/tutor",
            secondRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RegisterTutor_WithPasswordViolatingPolicy_ShouldReturnBadRequest()
    {
        var request = new
        {
            Email = "tutor@test.pl",
            UserName = "tutor1",
            Password = "password"
        };

        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/tutor",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
