using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.ScheduleLesson;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.ScheduleLesson;

public sealed class ScheduleLessonTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/me/lessons";

    [Fact]
    public async Task ScheduleLesson_ForActiveAgreement_ShouldCreateStandaloneScheduledLesson()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");
        var agreementId = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var startsAt = DateTimeOffset.UtcNow.AddDays(7).ToOffset(TimeSpan.FromHours(2));

        var response = await ScheduleLessonAsync(
            tutor.AccessToken,
            agreementId,
            startsAt,
            60);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ScheduleLessonResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.LessonId);

        var lesson = await ExecuteDbAsync(dbContext =>
            dbContext.Lessons
                .AsNoTracking()
                .SingleAsync(item => item.Id == new LessonId(result.LessonId)));

        Assert.Equal(agreementId, lesson.TutoringAgreementId.Value);
        Assert.Null(lesson.LessonSeriesId);
        Assert.Equal(LessonStatus.Scheduled, lesson.Status);
        Assert.Equal(startsAt.ToUniversalTime(), lesson.TimeSlot.StartsAtUtc);
        Assert.Equal(startsAt.ToUniversalTime().AddMinutes(60), lesson.TimeSlot.EndsAtUtc);
        Assert.NotEqual(default, lesson.CreatedAtUtc);
    }

    [Fact]
    public async Task ScheduleLesson_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            Endpoint,
            new
            {
                TutoringAgreementId = Guid.NewGuid(),
                StartsAt = DateTimeOffset.UtcNow.AddDays(7),
                DurationMinutes = 60
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ScheduleLesson_AsStudent_ShouldReturnForbidden()
    {
        var student = await CreateStudentAsync("student@test.pl", "student");

        var response = await ScheduleLessonAsync(
            student.AccessToken,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(7),
            60);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ScheduleLesson_WhenAgreementDoesNotExist_ShouldReturnNotFoundProblem()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");

        var response = await ScheduleLessonAsync(
            tutor.AccessToken,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(7),
            60);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, "TutoringAgreements.NotFound");
    }

    [Fact]
    public async Task ScheduleLesson_WhenAgreementBelongsToAnotherTutor_ShouldReturnNotFoundProblem()
    {
        var owner = await CreateTutorAsync("owner@test.pl", "owner");
        var otherTutor = await CreateTutorAsync("other@test.pl", "other");
        var student = await CreateStudentAsync("student@test.pl", "student");
        var agreementId = await CreateAgreementAsync(owner.Email, student.StudentId);

        var response = await ScheduleLessonAsync(
            otherTutor.AccessToken,
            agreementId,
            DateTimeOffset.UtcNow.AddDays(7),
            60);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, "TutoringAgreements.NotFound");
    }

    [Fact]
    public async Task ScheduleLesson_WhenAgreementIsSuspended_ShouldReturnNotActiveProblem()
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

        var response = await ScheduleLessonAsync(
            tutor.AccessToken,
            agreementId,
            DateTimeOffset.UtcNow.AddDays(7),
            60);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, "TutoringAgreements.NotActive");
        await AssertLessonCountAsync(0);
    }

    [Fact]
    public async Task ScheduleLesson_WithZeroDuration_ShouldReturnBadRequestAndNotPersistLesson()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");
        var agreementId = await CreateAgreementAsync(tutor.Email, student.StudentId);

        var response = await ScheduleLessonAsync(
            tutor.AccessToken,
            agreementId,
            DateTimeOffset.UtcNow.AddDays(7),
            0);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, "Request.Invalid");
        await AssertLessonCountAsync(0);
    }

    [Fact]
    public async Task ScheduleLesson_WithNegativeDuration_ShouldReturnBadRequestAndNotPersistLesson()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");
        var agreementId = await CreateAgreementAsync(tutor.Email, student.StudentId);

        var response = await ScheduleLessonAsync(
            tutor.AccessToken,
            agreementId,
            DateTimeOffset.UtcNow.AddDays(7),
            -60);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, "Request.Invalid");
        await AssertLessonCountAsync(0);
    }

    [Fact]
    public async Task ScheduleLesson_InThePast_ShouldReturnBadRequestAndNotPersistLesson()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");
        var agreementId = await CreateAgreementAsync(tutor.Email, student.StudentId);

        var response = await ScheduleLessonAsync(
            tutor.AccessToken,
            agreementId,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            60);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, "Request.Invalid");
        await AssertLessonCountAsync(0);
    }

    [Fact]
    public async Task ScheduleLesson_WhenPartiallyOverlappingExistingLesson_ShouldReturnConflict()
    {
        var (tutor, agreementId, startsAt) = await CreateTutorAgreementAndTimeAsync();
        await CreateLessonAsync(agreementId, startsAt, startsAt.AddHours(1));

        var response = await ScheduleLessonAsync(
            tutor.AccessToken,
            agreementId,
            startsAt.AddMinutes(30),
            60);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemCodeAsync(response, "Lessons.TimeConflict");
        await AssertLessonCountAsync(1);
    }

    [Fact]
    public async Task ScheduleLesson_WhenInsideExistingLesson_ShouldReturnConflict()
    {
        var (tutor, agreementId, startsAt) = await CreateTutorAgreementAndTimeAsync();
        await CreateLessonAsync(agreementId, startsAt, startsAt.AddHours(2));

        var response = await ScheduleLessonAsync(
            tutor.AccessToken,
            agreementId,
            startsAt.AddMinutes(30),
            30);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemCodeAsync(response, "Lessons.TimeConflict");
        await AssertLessonCountAsync(1);
    }

    [Fact]
    public async Task ScheduleLesson_WhenSurroundingExistingLesson_ShouldReturnConflict()
    {
        var (tutor, agreementId, startsAt) = await CreateTutorAgreementAndTimeAsync();
        await CreateLessonAsync(
            agreementId,
            startsAt.AddMinutes(30),
            startsAt.AddHours(1));

        var response = await ScheduleLessonAsync(
            tutor.AccessToken,
            agreementId,
            startsAt,
            120);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemCodeAsync(response, "Lessons.TimeConflict");
        await AssertLessonCountAsync(1);
    }

    [Fact]
    public async Task ScheduleLesson_BackToBackWithExistingLesson_ShouldCreateLesson()
    {
        var (tutor, agreementId, startsAt) = await CreateTutorAgreementAndTimeAsync();
        await CreateLessonAsync(agreementId, startsAt, startsAt.AddHours(1));

        var response = await ScheduleLessonAsync(
            tutor.AccessToken,
            agreementId,
            startsAt.AddHours(1),
            60);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await AssertLessonCountAsync(2);
    }

    [Fact]
    public async Task ScheduleLesson_ForAnotherTutorAtSameTime_ShouldCreateLesson()
    {
        var tutorA = await CreateTutorAsync("tutor-a@test.pl", "tutora");
        var tutorB = await CreateTutorAsync("tutor-b@test.pl", "tutorb");
        var studentA = await CreateStudentAsync("student-a@test.pl", "studenta");
        var studentB = await CreateStudentAsync("student-b@test.pl", "studentb");
        var agreementAId = await CreateAgreementAsync(tutorA.Email, studentA.StudentId);
        var agreementBId = await CreateAgreementAsync(tutorB.Email, studentB.StudentId);
        var startsAt = DateTimeOffset.UtcNow.AddDays(7);

        var firstResponse = await ScheduleLessonAsync(
            tutorA.AccessToken,
            agreementAId,
            startsAt,
            60);
        var secondResponse = await ScheduleLessonAsync(
            tutorB.AccessToken,
            agreementBId,
            startsAt,
            60);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        await AssertLessonCountAsync(2);
    }

    private async Task<(TestTutor Tutor, Guid AgreementId, DateTimeOffset StartsAt)>
        CreateTutorAgreementAndTimeAsync()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");
        var agreementId = await CreateAgreementAsync(tutor.Email, student.StudentId);

        return (tutor, agreementId, DateTimeOffset.UtcNow.AddDays(7));
    }

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

        return new TestTutor(email, accessToken);
    }

    private async Task<TestStudent> CreateStudentAsync(string email, string userName)
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

        return new TestStudent(accessToken, studentId);
    }

    private async Task<Guid> CreateAgreementAsync(string tutorEmail, Guid studentId)
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

    private async Task CreateLessonAsync(
        Guid agreementId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc)
    {
        await ExecuteDbAsync(async dbContext =>
        {
            dbContext.Lessons.Add(
                new Lesson(
                    new TutoringAgreementId(agreementId),
                    new TimeSlot(startsAtUtc, endsAtUtc),
                    DateTimeOffset.UtcNow));
            await dbContext.SaveChangesAsync();
        });
    }

    private Task<HttpResponseMessage> ScheduleLessonAsync(
        string accessToken,
        Guid agreementId,
        DateTimeOffset startsAt,
        int durationMinutes)
    {
        return Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                Endpoint,
                accessToken,
                new
                {
                    TutoringAgreementId = agreementId,
                    StartsAt = startsAt,
                    DurationMinutes = durationMinutes
                }));
    }

    private async Task AssertLessonCountAsync(int expectedCount)
    {
        var count = await ExecuteDbAsync(dbContext => dbContext.Lessons.CountAsync());
        Assert.Equal(expectedCount, count);
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.True(problem!.Extensions.TryGetValue("code", out var code));
        Assert.Equal(expectedCode, code?.ToString());
    }

    private sealed record TestTutor(string Email, string AccessToken);

    private sealed record TestStudent(string AccessToken, Guid StudentId);
}
