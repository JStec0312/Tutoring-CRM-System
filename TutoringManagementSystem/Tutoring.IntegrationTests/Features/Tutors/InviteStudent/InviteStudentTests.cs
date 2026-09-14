using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.InviteStudent;
using Tutoring.Domain.Billing;
using Tutoring.Domain.StudentInvitations;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Domain.Tutors;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.InviteStudent;

public sealed class InviteStudentTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/me/student-invitations";

    [Fact]
    public async Task InviteStudent_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            Endpoint,
            new
            {
                Email = "student@test.pl",
                Title = "Math tutoring",
                Subject = "Mathematics",
                HourlyRate = (decimal?)100
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InviteStudent_AsStudent_ShouldReturnForbidden()
    {
        var student = await CreateStudentAsync("student@test.pl", "student");

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                Endpoint,
                student.AccessToken,
                new
                {
                    Email = "other@test.pl",
                    Title = "Math tutoring",
                    Subject = "Mathematics",
                    HourlyRate = (decimal?)100
                }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InviteStudent_AsTutor_ShouldCreateInvitation()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                Endpoint,
                tutor.AccessToken,
                new
                {
                    Email = "student@test.pl",
                    Title = "Math tutoring",
                    Subject = "Mathematics",
                    HourlyRate = (decimal?)100
                }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<InviteStudentResponse>();
        Assert.NotNull(result);

        var invitation = await ExecuteDbAsync(dbContext =>
            dbContext.StudentInvitations
                .AsNoTracking()
                .Where(item => item.Id == new StudentInvitationId(result!.InvitationId))
                .SingleAsync());

        Assert.Equal(tutor.TutorId, invitation.TutorId.Value);
        Assert.Equal("student@test.pl", invitation.Recipient.Value);
        Assert.Equal("Math tutoring", invitation.Title.Value);
        Assert.Equal("Mathematics", invitation.Subject.Name);
        Assert.NotNull(invitation.HourlyRate);
        Assert.Equal(100, invitation.HourlyRate!.PricePerHour.Amount);
        Assert.Equal("PLN", invitation.HourlyRate.PricePerHour.Currency.Code);
        Assert.True(invitation.ValidUntilUtc > DateTimeOffset.UtcNow);
        Assert.Equal(InvitationStatus.Created, invitation.Status);
    }

    [Fact]
    public async Task InviteStudent_WithoutHourlyRate_ShouldCreateInvitationWithNullHourlyRate()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                Endpoint,
                tutor.AccessToken,
                new
                {
                    Email = "student@test.pl",
                    Title = "Math tutoring",
                    Subject = "Mathematics",
                    HourlyRate = (decimal?)null
                }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<InviteStudentResponse>();
        Assert.NotNull(result);

        var invitation = await ExecuteDbAsync(dbContext =>
            dbContext.StudentInvitations
                .AsNoTracking()
                .Where(item => item.Id == new StudentInvitationId(result!.InvitationId))
                .SingleAsync());

        Assert.Null(invitation.HourlyRate);
        Assert.Equal("student@test.pl", invitation.Recipient.Value);
        Assert.Equal("Math tutoring", invitation.Title.Value);
        Assert.Equal("Mathematics", invitation.Subject.Name);
    }

    [Fact]
    public async Task InviteStudent_ShouldNotPersistRawTokenAndShouldReturnUsableInvitationLink()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                Endpoint,
                tutor.AccessToken,
                new
                {
                    Email = "student@test.pl",
                    Title = "Math tutoring",
                    Subject = "Mathematics",
                    HourlyRate = (decimal?)null
                }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<InviteStudentResponse>();
        Assert.NotNull(result);

        var rawToken = ExtractTokenFromInvitationUrl(result!.InvitationUrl);

        var invitation = await ExecuteDbAsync(dbContext =>
            dbContext.StudentInvitations
                .AsNoTracking()
                .Where(item => item.Id == new StudentInvitationId(result.InvitationId))
                .SingleAsync());

        Assert.NotEqual(rawToken, invitation.TokenHash);

        // The raw token from the link must still resolve to the same invitation.
        var acceptResponse = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                $"/api/student-invitations/{Uri.EscapeDataString(rawToken)}/accept",
                student.AccessToken));

        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
    }

    [Fact]
    public async Task InviteStudent_SameTutorSameEmailTwice_ShouldCreateTwoInvitations()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");

        var request = new
        {
            Email = "student@test.pl",
            Title = "Math tutoring",
            Subject = "Mathematics",
            HourlyRate = (decimal?)100
        };

        var firstResponse = await Client.SendAsync(
            CreateAuthorizedRequest(HttpMethod.Post, Endpoint, tutor.AccessToken, request));
        var secondResponse = await Client.SendAsync(
            CreateAuthorizedRequest(HttpMethod.Post, Endpoint, tutor.AccessToken, request));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var invitationCount = await ExecuteDbAsync(dbContext =>
            dbContext.StudentInvitations
                .AsNoTracking()
                .Where(item =>
                    item.TutorId == new TutorId(tutor.TutorId) &&
                    item.Recipient.Value == "student@test.pl")
                .CountAsync());

        Assert.Equal(2, invitationCount);
    }

    [Fact]
    public async Task InviteStudent_WhenActiveAgreementAlreadyExists_ShouldReturnConflict()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");

        await CreateAgreementAsync(tutor.Email, student.Email);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                Endpoint,
                tutor.AccessToken,
                new
                {
                    Email = "student@test.pl",
                    Title = "Math tutoring",
                    Subject = "Mathematics",
                    HourlyRate = (decimal?)100
                }));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static string ExtractTokenFromInvitationUrl(string invitationUrl)
    {
        var uri = new Uri(invitationUrl);
        var rawSegment = uri.Segments[^1].TrimEnd('/');
        return Uri.UnescapeDataString(rawSegment);
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
                .Where(student => student.Account.Email.Value == email)
                .Select(student => student.Id.Value)
                .SingleAsync());

        return new TestStudent(email, accessToken, studentId);
    }

    private async Task CreateAgreementAsync(
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
                .Where(student => student.Account.Email.Value == studentEmail)
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

    private sealed record TestTutor(
        string Email,
        string AccessToken,
        Guid TutorId);

    private sealed record TestStudent(
        string Email,
        string AccessToken,
        Guid StudentId);
}
