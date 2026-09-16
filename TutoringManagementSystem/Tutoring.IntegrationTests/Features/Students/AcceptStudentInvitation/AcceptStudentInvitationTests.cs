using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Students.AcceptStudentInvitation;
using Tutoring.Api.Features.Tutors.InviteStudent;
using Tutoring.Domain.Billing;
using Tutoring.Domain.StudentInvitations;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Domain.Tutors;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Students.AcceptStudentInvitation;

public sealed class AcceptStudentInvitationTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string InviteEndpoint = "/api/tutors/me/student-invitations";

    [Fact]
    public async Task Accept_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var response = await Client.PostAsync(
            $"/api/student-invitations/{Uri.EscapeDataString("whatever")}/accept",
            null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Accept_AsTutor_ShouldReturnForbidden()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                $"/api/student-invitations/{Uri.EscapeDataString("whatever")}/accept",
                tutor.AccessToken));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Accept_AsCorrectStudent_ShouldCreateAgreementAndMarkInvitationAccepted()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");

        var invitation = await CreateInvitationAsync(
            tutor.AccessToken,
            "student@test.pl",
            "Math tutoring",
            "Mathematics",
            100);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                AcceptUrl(invitation.RawToken),
                student.AccessToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<AcceptStudentInvitationResponse>();
        Assert.NotNull(result);

        var storedInvitation = await ExecuteDbAsync(dbContext =>
            dbContext.StudentInvitations
                .AsNoTracking()
                .Where(item => item.Id == new StudentInvitationId(invitation.InvitationId))
                .SingleAsync());

        Assert.Equal(InvitationStatus.Accepted, storedInvitation.Status);

        var agreements = await ExecuteDbAsync(dbContext =>
            dbContext.TutoringAgreements
                .AsNoTracking()
                .Where(agreement =>
                    agreement.TutorId == new TutorId(tutor.TutorId) &&
                    agreement.StudentId == new StudentId(student.StudentId))
                .ToListAsync());

        var agreement = Assert.Single(agreements);
        Assert.Equal(result!.AgreementId, agreement.Id.Value);
        Assert.Equal(tutor.TutorId, agreement.TutorId.Value);
        Assert.Equal(student.StudentId, agreement.StudentId.Value);
        Assert.Equal("Math tutoring", agreement.AgreementTitle.Value);
        Assert.Equal("Mathematics", agreement.Subject.Name);
        Assert.NotNull(agreement.HourlyRate);
        Assert.Equal(100, agreement.HourlyRate!.PricePerHour.Amount);
        Assert.Equal(AgreementStatus.Active, agreement.Status);
    }

    [Fact]
    public async Task Accept_WithNullHourlyRate_ShouldCreateAgreementWithNullHourlyRate()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");

        var invitation = await CreateInvitationAsync(
            tutor.AccessToken,
            "student@test.pl",
            "Math tutoring",
            "Mathematics",
            null);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                AcceptUrl(invitation.RawToken),
                student.AccessToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var agreement = await ExecuteDbAsync(dbContext =>
            dbContext.TutoringAgreements
                .AsNoTracking()
                .Where(item =>
                    item.TutorId == new TutorId(tutor.TutorId) &&
                    item.StudentId == new StudentId(student.StudentId))
                .SingleAsync());

        Assert.Null(agreement.HourlyRate);
    }

    [Fact]
    public async Task Accept_WithUnknownToken_ShouldReturnNotFound()
    {
        var student = await CreateStudentAsync("student@test.pl", "student");

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                AcceptUrl("does-not-exist-token"),
                student.AccessToken));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Accept_WhenExpired_ShouldReturnConflict()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");

        var invitation = await CreateInvitationAsync(
            tutor.AccessToken,
            "student@test.pl",
            "Math tutoring",
            "Mathematics",
            100);

        await SetInvitationValidUntilAsync(
            invitation.InvitationId,
            DateTimeOffset.UtcNow.AddDays(-1));

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                AcceptUrl(invitation.RawToken),
                student.AccessToken));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Accept_WhenAlreadyAccepted_ShouldReturnConflictOnSecondAttempt()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");

        var invitation = await CreateInvitationAsync(
            tutor.AccessToken,
            "student@test.pl",
            "Math tutoring",
            "Mathematics",
            100);

        var firstResponse = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                AcceptUrl(invitation.RawToken),
                student.AccessToken));
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                AcceptUrl(invitation.RawToken),
                student.AccessToken));

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Accept_WhenRejected_ShouldReturnConflict()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");

        var invitation = await CreateInvitationAsync(
            tutor.AccessToken,
            "student@test.pl",
            "Math tutoring",
            "Mathematics",
            100);

        await SetInvitationStatusAsync(
            invitation.InvitationId,
            InvitationStatus.Rejected);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                AcceptUrl(invitation.RawToken),
                student.AccessToken));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Accept_WithDifferentRecipientEmail_ShouldReturnForbidden()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        await CreateStudentAsync("student@test.pl", "student");
        var otherStudent = await CreateStudentAsync("other-student@test.pl", "other-student");

        var invitation = await CreateInvitationAsync(
            tutor.AccessToken,
            "student@test.pl",
            "Math tutoring",
            "Mathematics",
            100);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                AcceptUrl(invitation.RawToken),
                otherStudent.AccessToken));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Accept_WithoutStudentProfile_ShouldReturnNotFound()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");

        var invitation = await CreateInvitationAsync(
            tutor.AccessToken,
            "student@test.pl",
            "Math tutoring",
            "Mathematics",
            100);

        await ExecuteDbAsync(async dbContext =>
        {
            var studentEntity = await dbContext.Students
                .Where(item => item.Id == new StudentId(student.StudentId))
                .SingleAsync();
            dbContext.Students.Remove(studentEntity);
            await dbContext.SaveChangesAsync();
        });

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                AcceptUrl(invitation.RawToken),
                student.AccessToken));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Accept_WhenActiveAgreementAlreadyExistsForTutorAndStudent_ShouldReturnConflict()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");

        var invitation = await CreateInvitationAsync(
            tutor.AccessToken,
            "student@test.pl",
            "Math tutoring",
            "Mathematics",
            100);

        await CreateAgreementDirectlyAsync(tutor.Email, student.Email);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                AcceptUrl(invitation.RawToken),
                student.AccessToken));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var agreementCount = await ExecuteDbAsync(dbContext =>
            dbContext.TutoringAgreements
                .AsNoTracking()
                .Where(item =>
                    item.TutorId == new TutorId(tutor.TutorId) &&
                    item.StudentId == new StudentId(student.StudentId))
                .CountAsync());

        Assert.Equal(1, agreementCount);
    }

    [Fact]
    public async Task Accept_TwoInvitationsForSameTutorAndStudent_ShouldResultInOnlyOneActiveAgreement()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");

        var firstInvitation = await CreateInvitationAsync(
            tutor.AccessToken,
            "student@test.pl",
            "Math tutoring",
            "Mathematics",
            100);
        var secondInvitation = await CreateInvitationAsync(
            tutor.AccessToken,
            "student@test.pl",
            "Physics tutoring",
            "Physics",
            120);

        var firstAcceptResponse = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                AcceptUrl(firstInvitation.RawToken),
                student.AccessToken));
        Assert.Equal(HttpStatusCode.OK, firstAcceptResponse.StatusCode);

        var secondAcceptResponse = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                AcceptUrl(secondInvitation.RawToken),
                student.AccessToken));

        Assert.Equal(HttpStatusCode.Conflict, secondAcceptResponse.StatusCode);

        var activeAgreements = await ExecuteDbAsync(dbContext =>
            dbContext.TutoringAgreements
                .AsNoTracking()
                .Where(item =>
                    item.TutorId == new TutorId(tutor.TutorId) &&
                    item.StudentId == new StudentId(student.StudentId) &&
                    item.Status != AgreementStatus.Ended)
                .ToListAsync());

        Assert.Single(activeAgreements);
    }

    private static string AcceptUrl(string rawToken) =>
        $"/api/student-invitations/{Uri.EscapeDataString(rawToken)}/accept";

    private static string ExtractTokenFromInvitationUrl(string invitationUrl)
    {
        var uri = new Uri(invitationUrl);
        var rawSegment = uri.Segments[^1].TrimEnd('/');
        return Uri.UnescapeDataString(rawSegment);
    }

    private async Task<TestInvitation> CreateInvitationAsync(
        string tutorAccessToken,
        string email,
        string title,
        string subject,
        decimal? hourlyRate)
    {
        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                InviteEndpoint,
                tutorAccessToken,
                new
                {
                    Email = email,
                    Title = title,
                    Subject = subject,
                    HourlyRate = hourlyRate
                }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<InviteStudentResponse>();
        Assert.NotNull(result);

        return new TestInvitation(
            result!.InvitationId,
            ExtractTokenFromInvitationUrl(result.InvitationUrl));
    }

    private async Task SetInvitationValidUntilAsync(
        Guid invitationId,
        DateTimeOffset validUntilUtc)
    {
        await ExecuteDbAsync(async dbContext =>
        {
            var invitation = await dbContext.StudentInvitations
                .Where(item => item.Id == new StudentInvitationId(invitationId))
                .SingleAsync();
            dbContext.Entry(invitation)
                .Property(item => item.ValidUntilUtc)
                .CurrentValue = validUntilUtc;
            await dbContext.SaveChangesAsync();
        });
    }

    private async Task SetInvitationStatusAsync(
        Guid invitationId,
        InvitationStatus status)
    {
        await ExecuteDbAsync(async dbContext =>
        {
            var invitation = await dbContext.StudentInvitations
                .Where(item => item.Id == new StudentInvitationId(invitationId))
                .SingleAsync();
            dbContext.Entry(invitation)
                .Property(item => item.Status)
                .CurrentValue = status;
            await dbContext.SaveChangesAsync();
        });
    }

    private async Task CreateAgreementDirectlyAsync(
        string tutorEmail,
        string studentEmail)
    {
        await ExecuteDbAsync(async dbContext =>
        {
            var tutorId = await dbContext.Tutors
                .Where(tutor => tutor.Account.Email.Value == tutorEmail)
                .Select(tutor => tutor.Id)
                .SingleAsync();
            var studentId = await dbContext.Students
                .Where(student =>
                    student.Account != null &&
                    student.Account.Email.Value == studentEmail)
                .Select(student => student.Id)
                .SingleAsync();

            var agreement = new TutoringAgreement(
                tutorId,
                studentId,
                new Subject("Mathematics"),
                new HourlyRate(new Money(100, new Currency("PLN"))),
                new AgreementTitle("Existing agreement"),
                DateTimeOffset.UtcNow);

            dbContext.TutoringAgreements.Add(agreement);
            await dbContext.SaveChangesAsync();
        });
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
                .Where(student =>
                    student.Account != null &&
                    student.Account.Email.Value == email)
                .Select(student => student.Id.Value)
                .SingleAsync());

        return new TestStudent(email, accessToken, studentId);
    }

    private sealed record TestInvitation(
        Guid InvitationId,
        string RawToken);

    private sealed record TestTutor(
        string Email,
        string AccessToken,
        Guid TutorId);

    private sealed record TestStudent(
        string Email,
        string AccessToken,
        Guid StudentId);
}
