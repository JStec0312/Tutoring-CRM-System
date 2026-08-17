using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Auth;

public sealed class RegisterStudentTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task RegisterStudent_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var request = new
        {
            Email = "student@test.pl",
            Username = "student1",
            Password = "Password123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/student",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var accountExists = await ExecuteDbAsync<bool>(dbContext =>
            dbContext.Students
                .AnyAsync(s => s.Account.Email.Value.Equals(request.Email))
        );

        Assert.True(accountExists);
    }
}