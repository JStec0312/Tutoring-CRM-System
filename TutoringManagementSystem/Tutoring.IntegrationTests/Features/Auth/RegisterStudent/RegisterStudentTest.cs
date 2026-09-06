using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Identity;
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

        var account = await ExecuteDbAsync(dbContext =>
            dbContext.UserAccounts.SingleAsync(account =>
                account.Email.Value == request.Email));
        var verificationToken = await ExecuteDbAsync(dbContext =>
            dbContext.EmailVerificationTokens.SingleAsync(token =>
                token.UserAccountId == account.Id));

        Assert.Equal(AccountStatus.PendingActivation, account.Status);
        Assert.Equal(account.Id, verificationToken.UserAccountId);
        Assert.False(string.IsNullOrWhiteSpace(
            await GetVerificationTokenAsync(request.Email)));
    }

    [Fact]
    public async Task RegisterStudent_WithDuplicateEmail_ShouldReturnConflict()
    {
        // Arrange
        var firstRequest = new
        {
            Email = "student@test.pl",
            Username = "student1",
            Password = "Password123!"
        };

        var secondRequest = new
        {
            Email = "student@test.pl",
            Username = "student2",
            Password = "Password123!"
        };

        await Client.PostAsJsonAsync(
            "/api/auth/register/student",
            firstRequest);

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/student",
            secondRequest);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task RegisterStudent_WithValidData_ShouldSaveCorrectStudent()
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

        var student = await ExecuteDbAsync(dbContext =>
            dbContext.Students
                .Include(s => s.Account)
                .SingleAsync());

        Assert.Equal(
            request.Email,
            student.Account.Email.Value);

        Assert.Equal(
            request.Username,
            student.Account.Profile.UserName);
    }
}