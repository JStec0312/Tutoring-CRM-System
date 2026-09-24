using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.AddStudentManually;
using Tutoring.Api.Features.Tutors.GetAssignedStudents;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Domain.Tutors;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.AddStudentManually;

[Collection(IntegrationTestCollection.Name)]
public sealed class AddStudentManuallyTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/me/students";

    [Fact]
    public async Task AddStudentManually_AsTutor_ShouldCreateManagedStudentAndAgreement()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var accountsBefore = await ExecuteDbAsync(dbContext =>
            dbContext.UserAccounts.CountAsync());

        var response = await AddStudentManuallyAsync(
            tutor.AccessToken,
            "Jasio Kowalski",
            "Jasio matematyka",
            "Matematyka",
            80);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content
            .ReadFromJsonAsync<AddStudentManuallyResponse>();
        Assert.NotNull(result);

        var students = await ExecuteDbAsync(dbContext =>
            dbContext.Students
                .AsNoTracking()
                .Include(student => student.Account)
                .ToListAsync());
        var student = Assert.Single(students);

        Assert.Equal(result!.StudentId, student.Id.Value);
        Assert.Equal("Jasio Kowalski", student.DisplayName.Value);
        Assert.Null(student.UserAccountId);
        Assert.Null(student.Account);
        Assert.Equal(StudentStatus.Active, student.Status);
        Assert.NotEqual(default, student.CreatedAtUtc);

        var agreement = await ExecuteDbAsync(dbContext =>
            dbContext.TutoringAgreements
                .AsNoTracking()
                .SingleAsync(item => item.Id == new TutoringAgreementId(result.AgreementId)));

        Assert.Equal(tutor.TutorId, agreement.TutorId.Value);
        Assert.Equal(result.StudentId, agreement.StudentId.Value);
        Assert.Equal("Jasio matematyka", agreement.AgreementTitle.Value);
        Assert.Equal("Matematyka", agreement.Subject.Name);
        Assert.NotNull(agreement.HourlyRate);
        Assert.Equal(80, agreement.HourlyRate!.PricePerHour.Amount);
        Assert.Equal("PLN", agreement.HourlyRate.PricePerHour.Currency.Code);
        Assert.Equal(AgreementStatus.Active, agreement.Status);

        var accountsAfter = await ExecuteDbAsync(dbContext =>
            dbContext.UserAccounts.CountAsync());
        Assert.Equal(accountsBefore, accountsAfter);
    }

    [Fact]
    public async Task AddStudentManually_WithoutHourlyRate_ShouldCreateAgreementWithoutRate()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");

        var response = await AddStudentManuallyAsync(
            tutor.AccessToken,
            "Jasio Kowalski",
            "Jasio matematyka",
            "Matematyka",
            null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content
            .ReadFromJsonAsync<AddStudentManuallyResponse>();
        Assert.NotNull(result);

        var agreement = await ExecuteDbAsync(dbContext =>
            dbContext.TutoringAgreements
                .AsNoTracking()
                .SingleAsync(item => item.Id == new TutoringAgreementId(result!.AgreementId)));

        Assert.Equal(tutor.TutorId, agreement.TutorId.Value);
        Assert.Equal(result!.StudentId, agreement.StudentId.Value);
        Assert.Equal("Jasio matematyka", agreement.AgreementTitle.Value);
        Assert.Equal("Matematyka", agreement.Subject.Name);
        Assert.Null(agreement.HourlyRate);
        Assert.Equal(AgreementStatus.Active, agreement.Status);
    }

    [Fact]
    public async Task AddStudentManually_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            Endpoint,
            ValidRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddStudentManually_AsStudent_ShouldReturnForbidden()
    {
        var student = await CreateStudentAsync("student@test.pl", "student");

        var response = await AddStudentManuallyAsync(
            student.AccessToken,
            "Jasio Kowalski",
            "Jasio matematyka",
            "Matematyka",
            80);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddStudentManually_WhenTutorRecordIsMissing_ShouldReturnNotFoundProblem()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");

        await ExecuteDbAsync(async dbContext =>
        {
            var tutorEntity = await dbContext.Tutors
                .SingleAsync(item => item.Id == new TutorId(tutor.TutorId));
            dbContext.Tutors.Remove(tutorEntity);
            await dbContext.SaveChangesAsync();
        });

        var response = await AddStudentManuallyAsync(
            tutor.AccessToken,
            "Jasio Kowalski",
            "Jasio matematyka",
            "Matematyka",
            80);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem!.Extensions.TryGetValue("code", out var code));
        Assert.NotNull(code);
        Assert.Equal("Tutors.NotFound", code.ToString());
    }

    [Theory]
    [InlineData("", "Jasio matematyka", "Matematyka", 80)]
    [InlineData("Jasio Kowalski", "", "Matematyka", 80)]
    [InlineData("Jasio Kowalski", "Jasio matematyka", "", 80)]
    [InlineData("Jasio Kowalski", "Jasio matematyka", "Matematyka", -1)]
    public async Task AddStudentManually_WithInvalidValueObjectData_ShouldReturnBadRequest(
        string displayName,
        string title,
        string subject,
        decimal hourlyRate)
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");

        var response = await AddStudentManuallyAsync(
            tutor.AccessToken,
            displayName,
            title,
            subject,
            hourlyRate);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem!.Extensions.TryGetValue("code", out var code));
        Assert.NotNull(code);
        Assert.Equal("Request.Invalid", code.ToString());
    }

    [Fact]
    public async Task AddStudentManually_ShouldAppearInAssignedStudentsWithNullAccountFields()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");

        var addResponse = await AddStudentManuallyAsync(
            tutor.AccessToken,
            "Jasio Kowalski",
            "Jasio matematyka",
            "Matematyka",
            80);
        var addedStudent = await addResponse.Content
            .ReadFromJsonAsync<AddStudentManuallyResponse>();
        Assert.NotNull(addedStudent);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(HttpMethod.Get, Endpoint, tutor.AccessToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var students = await response.Content
            .ReadFromJsonAsync<List<AssignedStudentResponse>>();
        var student = Assert.Single(students!);

        Assert.Equal(addedStudent!.StudentId, student.StudentId);
        Assert.Equal("Jasio Kowalski", student.DisplayName);
        Assert.Null(student.FirstName);
        Assert.Null(student.LastName);
        Assert.Null(student.Email);
        Assert.Null(student.PhoneNumber);
        Assert.Equal(StudentStatus.Active.ToString(), student.Status);
    }

    private async Task<HttpResponseMessage> AddStudentManuallyAsync(
        string accessToken,
        string displayName,
        string title,
        string subject,
        decimal? hourlyRate) =>
        await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                Endpoint,
                accessToken,
                new
                {
                    DisplayName = displayName,
                    Title = title,
                    Subject = subject,
                    HourlyRate = hourlyRate
                }));

    private static object ValidRequest() => new
    {
        DisplayName = "Jasio Kowalski",
        Title = "Jasio matematyka",
        Subject = "Matematyka",
        HourlyRate = (decimal?)80
    };

    private async Task<TestTutor> CreateTutorAsync(string email, string userName)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/tutor",
            new
            {
                Email = email,
                UserName = userName,
                Password,
                FirstName = "Tutor",
                LastName = "Test",
                PhoneNumber = (string?)null
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await ConfirmEmailAsync(await GetVerificationTokenAsync(email));
        var accessToken = (await LoginAsync(email, Password)).Login.AccessToken;
        var tutorId = await ExecuteDbAsync(dbContext =>
            dbContext.Tutors
                .Where(tutor => tutor.Account.Email.Value == email)
                .Select(tutor => tutor.Id.Value)
                .SingleAsync());

        return new TestTutor(accessToken, tutorId);
    }

    private async Task<TestStudent> CreateStudentAsync(string email, string userName)
    {
        await RegisterAndConfirmStudentAsync(email, Password);
        var accessToken = (await LoginAsync(email, Password)).Login.AccessToken;

        return new TestStudent(accessToken);
    }

    private sealed record TestTutor(string AccessToken, Guid TutorId);

    private sealed record TestStudent(string AccessToken);
}
