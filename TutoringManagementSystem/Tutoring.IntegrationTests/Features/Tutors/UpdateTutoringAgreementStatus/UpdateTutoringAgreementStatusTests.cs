using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.UpdateTutoringAgreementStatus;

public sealed class UpdateTutoringAgreementStatusTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/tutoring-agreements";

    [Fact]
    public async Task UpdateTutoringAgreementStatus_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var response = await Client.PutAsJsonAsync(
            $"{Endpoint}/{Guid.NewGuid()}/status",
            new { IsActive = false });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTutoringAgreementStatus_AsStudent_ShouldReturnForbidden()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");

        var agreementId = await CreateAgreementAsync(tutor.Email, student.StudentId);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Put,
                $"{Endpoint}/{agreementId}/status",
                student.AccessToken,
                new { IsActive = false }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTutoringAgreementStatus_WhenAgreementIsActive_ShouldSuspendAgreement()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");

        var agreementId = await CreateAgreementAsync(tutor.Email, student.StudentId);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Put,
                $"{Endpoint}/{agreementId}/status",
                tutor.AccessToken,
                new { IsActive = false }));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var agreement = await ExecuteDbAsync(dbContext =>
            dbContext.TutoringAgreements
                .AsNoTracking()
                .SingleAsync(item => item.Id == new TutoringAgreementId(agreementId)));

        Assert.Equal(AgreementStatus.Suspended, agreement.Status);
    }

    [Fact]
    public async Task UpdateTutoringAgreementStatus_WhenAgreementIsSuspended_ShouldActivateAgreement()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");

        var agreementId = await CreateAgreementAsync(tutor.Email, student.StudentId);

        await ExecuteDbAsync(async dbContext =>
        {
            var agreement = await dbContext.TutoringAgreements
                .SingleAsync(item => item.Id == new TutoringAgreementId(agreementId));

            agreement.Deactivate();
            await dbContext.SaveChangesAsync();
        });

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Put,
                $"{Endpoint}/{agreementId}/status",
                tutor.AccessToken,
                new { IsActive = true }));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var agreement = await ExecuteDbAsync(dbContext =>
            dbContext.TutoringAgreements
                .AsNoTracking()
                .SingleAsync(item => item.Id == new TutoringAgreementId(agreementId)));

        Assert.Equal(AgreementStatus.Active, agreement.Status);
    }

    [Fact]
    public async Task UpdateTutoringAgreementStatus_WhenAgreementBelongsToAnotherTutor_ShouldReturnNotFound()
    {
        var owner = await CreateTutorAsync("owner@test.pl", "owner");
        var otherTutor = await CreateTutorAsync("other@test.pl", "other");
        var student = await CreateStudentAsync("student@test.pl", "student");

        var agreementId = await CreateAgreementAsync(owner.Email, student.StudentId);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Put,
                $"{Endpoint}/{agreementId}/status",
                otherTutor.AccessToken,
                new { IsActive = false }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTutoringAgreementStatus_WhenAgreementDoesNotExist_ShouldReturnNotFound()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Put,
                $"{Endpoint}/{Guid.NewGuid()}/status",
                tutor.AccessToken,
                new { IsActive = false }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<TestTutor> CreateTutorAsync(
        string email,
        string userName)
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

        return new TestTutor(email, accessToken, tutorId);
    }

    private async Task<TestStudent> CreateStudentAsync(
        string email,
        string userName)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/student",
            new
            {
                Email = email,
                Username = userName,
                Password
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await ConfirmEmailAsync(await GetVerificationTokenAsync(email));
        var accessToken = (await LoginAsync(email, Password)).Login.AccessToken;

        var studentId = await ExecuteDbAsync(dbContext =>
            dbContext.Students
                .Where(student => student.Account != null && student.Account.Email.Value == email)
                .Select(student => student.Id.Value)
                .SingleAsync());

        return new TestStudent(email, accessToken, studentId);
    }

    private async Task<Guid> CreateAgreementAsync(
        string tutorEmail,
        Guid studentId)
    {
        return await ExecuteDbAsync(async dbContext =>
        {
            var tutorId = await dbContext.Tutors
                .Where(tutor => tutor.Account.Email.Value == tutorEmail)
                .Select(tutor => tutor.Id)
                .SingleAsync();

            var agreement = new TutoringAgreement(
                tutorId,
                new StudentId(studentId),
                new Subject("Mathematics"),
                new HourlyRate(new Money(100, new Currency("PLN"))),
                new AgreementTitle("Test Agreement"),
                DateTimeOffset.UtcNow);

            dbContext.TutoringAgreements.Add(agreement);
            await dbContext.SaveChangesAsync();

            return agreement.Id.Value;
        });
    }

    private sealed record TestTutor(
        string Email,
        string AccessToken,
        Guid TutorId);

    private sealed record TestStudent(
        string Email,
        string AccessToken,
        Guid StudentId);
}
