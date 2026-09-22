using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Billing;
using Tutoring.Domain.LessonSeries;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.RescheduleLesson;

public sealed class RescheduleLessonTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/me/lessons";
    private static readonly DateTimeOffset InitialStartsAtUtc = new(2099, 9, 25, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RescheduleLesson_ForOwnedScheduledLesson_ShouldUpdateOnlyTimeSlot()
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        var lessonId = await CreateLessonAsync(
            agreementId,
            InitialStartsAtUtc,
            InitialStartsAtUtc.AddHours(1),
            LessonSeriesId.New());
        var before = await GetLessonAsync(lessonId);
        var requestedStartsAt = new DateTimeOffset(2099, 9, 25, 18, 0, 0, TimeSpan.FromHours(2));

        var response = await RescheduleLessonAsync(
            tutor.AccessToken,
            lessonId,
            requestedStartsAt,
            60);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var after = await GetLessonAsync(lessonId);
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(before.TutoringAgreementId, after.TutoringAgreementId);
        Assert.Equal(before.LessonSeriesId, after.LessonSeriesId);
        Assert.Equal(before.CreatedAtUtc, after.CreatedAtUtc);
        Assert.Equal(before.Status, after.Status);
        Assert.Equal(requestedStartsAt.ToUniversalTime(), after.StartsAtUtc);
        Assert.Equal(requestedStartsAt.ToUniversalTime().AddMinutes(60), after.EndsAtUtc);
    }

    [Fact]
    public async Task RescheduleLesson_WhenLessonDoesNotExist_ShouldReturnNotFoundProblem()
    {
        var (tutor, _) = await CreateTutorAndAgreementAsync();

        var response = await RescheduleLessonAsync(
            tutor.AccessToken,
            Guid.NewGuid(),
            InitialStartsAtUtc,
            60);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, "Lessons.NotFound");
    }

    [Fact]
    public async Task RescheduleLesson_WhenLessonBelongsToAnotherTutor_ShouldReturnSameNotFoundProblem()
    {
        var (owner, agreementId) = await CreateTutorAndAgreementAsync();
        var otherTutor = await CreateTutorAsync("other-tutor@test.pl", "other-tutor");
        var lessonId = await CreateLessonAsync(
            agreementId,
            InitialStartsAtUtc,
            InitialStartsAtUtc.AddHours(1));
        var before = await GetLessonAsync(lessonId);

        var response = await RescheduleLessonAsync(
            otherTutor.AccessToken,
            lessonId,
            InitialStartsAtUtc.AddDays(1),
            60);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, "Lessons.NotFound");
        Assert.Equal(before, await GetLessonAsync(lessonId));
        Assert.NotEqual(owner.AccessToken, otherTutor.AccessToken);
    }

    [Fact]
    public async Task RescheduleLesson_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var response = await Client.PutAsJsonAsync(
            $"{Endpoint}/{Guid.NewGuid()}/schedule",
            new
            {
                StartsAt = InitialStartsAtUtc,
                DurationMinutes = 60
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RescheduleLesson_AsStudent_ShouldReturnForbidden()
    {
        var student = await CreateStudentAsync("student@test.pl", "student");

        var response = await RescheduleLessonAsync(
            student.AccessToken,
            Guid.NewGuid(),
            InitialStartsAtUtc,
            60);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public async Task RescheduleLesson_WithNonPositiveDuration_ShouldReturnBadRequestAndNotModifyLesson(
        int durationMinutes)
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        var lessonId = await CreateLessonAsync(
            agreementId,
            InitialStartsAtUtc,
            InitialStartsAtUtc.AddHours(1));
        var before = await GetLessonAsync(lessonId);

        var response = await RescheduleLessonAsync(
            tutor.AccessToken,
            lessonId,
            InitialStartsAtUtc.AddDays(1),
            durationMinutes);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, "Request.Invalid");
        Assert.Equal(before, await GetLessonAsync(lessonId));
    }

    [Theory]
    [InlineData(2000, 1, 1, 10, 0, 0)]
    public async Task RescheduleLesson_WhenStartsAtIsNotInFuture_ShouldReturnBadRequestAndNotModifyLesson(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second)
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        var lessonId = await CreateLessonAsync(
            agreementId,
            InitialStartsAtUtc,
            InitialStartsAtUtc.AddHours(1));
        var before = await GetLessonAsync(lessonId);
        var startsAt = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);

        var response = await RescheduleLessonAsync(
            tutor.AccessToken,
            lessonId,
            startsAt,
            60);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, "Request.Invalid");
        Assert.Equal(before, await GetLessonAsync(lessonId));
    }

    [Fact]
    public async Task RescheduleLesson_WhenStartsAtEqualsNow_ShouldReturnBadRequestAndNotModifyLesson()
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        var lessonId = await CreateLessonAsync(
            agreementId,
            InitialStartsAtUtc,
            InitialStartsAtUtc.AddHours(1));
        var before = await GetLessonAsync(lessonId);

        var response = await RescheduleLessonAsync(
            tutor.AccessToken,
            lessonId,
            DateTimeOffset.UtcNow,
            60);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, "Request.Invalid");
        Assert.Equal(before, await GetLessonAsync(lessonId));
    }

    [Fact]
    public async Task RescheduleLesson_WhenOverlappingAnotherScheduledLesson_ShouldReturnConflictAndNotModifyLesson()
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        var lessonId = await CreateLessonAsync(
            agreementId,
            InitialStartsAtUtc.AddHours(3),
            InitialStartsAtUtc.AddHours(4));
        await CreateLessonAsync(
            agreementId,
            InitialStartsAtUtc,
            InitialStartsAtUtc.AddHours(1));
        var before = await GetLessonAsync(lessonId);

        var response = await RescheduleLessonAsync(
            tutor.AccessToken,
            lessonId,
            InitialStartsAtUtc.AddMinutes(30),
            60);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemCodeAsync(response, "Lessons.TimeConflict");
        Assert.Equal(before, await GetLessonAsync(lessonId));
    }

    [Fact]
    public async Task RescheduleLesson_ToItsExistingTimeSlot_ShouldNotConflictWithItself()
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        var lessonId = await CreateLessonAsync(
            agreementId,
            InitialStartsAtUtc,
            InitialStartsAtUtc.AddHours(1));

        var response = await RescheduleLessonAsync(
            tutor.AccessToken,
            lessonId,
            InitialStartsAtUtc,
            60);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RescheduleLesson_BackToBackWithAnotherScheduledLesson_ShouldNotConflict()
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        var lessonId = await CreateLessonAsync(
            agreementId,
            InitialStartsAtUtc.AddHours(3),
            InitialStartsAtUtc.AddHours(4));
        await CreateLessonAsync(
            agreementId,
            InitialStartsAtUtc,
            InitialStartsAtUtc.AddHours(1));

        var response = await RescheduleLessonAsync(
            tutor.AccessToken,
            lessonId,
            InitialStartsAtUtc.AddHours(1),
            60);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RescheduleLesson_WhenOnlyAnotherTutorsLessonOverlaps_ShouldNotConflict()
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        var (_, otherAgreementId) = await CreateTutorAndAgreementAsync(
            "other-tutor@test.pl",
            "other-tutor",
            "other-student@test.pl",
            "other-student");
        var lessonId = await CreateLessonAsync(
            agreementId,
            InitialStartsAtUtc.AddHours(3),
            InitialStartsAtUtc.AddHours(4));
        await CreateLessonAsync(
            otherAgreementId,
            InitialStartsAtUtc,
            InitialStartsAtUtc.AddHours(1));

        var response = await RescheduleLessonAsync(
            tutor.AccessToken,
            lessonId,
            InitialStartsAtUtc.AddMinutes(30),
            60);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData(LessonStatus.Completed)]
    [InlineData(LessonStatus.Cancelled)]
    [InlineData(LessonStatus.Missed)]
    public async Task RescheduleLesson_WhenOnlyInactiveLessonOverlaps_ShouldNotConflict(
        LessonStatus inactiveStatus)
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        var lessonId = await CreateLessonAsync(
            agreementId,
            InitialStartsAtUtc.AddHours(3),
            InitialStartsAtUtc.AddHours(4));
        var inactiveLessonId = await CreateLessonAsync(
            agreementId,
            InitialStartsAtUtc,
            InitialStartsAtUtc.AddHours(1));
        await SetLessonStatusAsync(inactiveLessonId, inactiveStatus);

        var response = await RescheduleLessonAsync(
            tutor.AccessToken,
            lessonId,
            InitialStartsAtUtc.AddMinutes(30),
            60);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData(LessonStatus.Completed)]
    [InlineData(LessonStatus.Cancelled)]
    [InlineData(LessonStatus.Missed)]
    public async Task RescheduleLesson_WhenLessonIsNotScheduled_ShouldReturnBusinessRuleProblemAndNotModifyLesson(
        LessonStatus status)
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        var lessonId = await CreateLessonAsync(
            agreementId,
            InitialStartsAtUtc,
            InitialStartsAtUtc.AddHours(1));
        await SetLessonStatusAsync(lessonId, status);
        var before = await GetLessonAsync(lessonId);

        var response = await RescheduleLessonAsync(
            tutor.AccessToken,
            lessonId,
            InitialStartsAtUtc.AddDays(1),
            60);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        await AssertProblemCodeAsync(response, "Lessons.CannotBeRescheduled");
        Assert.Equal(before, await GetLessonAsync(lessonId));
    }

    private async Task<(TestTutor Tutor, Guid AgreementId)> CreateTutorAndAgreementAsync(
        string tutorEmail = "tutor@test.pl",
        string tutorUserName = "tutor",
        string studentEmail = "student@test.pl",
        string studentUserName = "student")
    {
        var tutor = await CreateTutorAsync(tutorEmail, tutorUserName);
        var student = await CreateStudentAsync(studentEmail, studentUserName);
        var agreementId = await CreateAgreementAsync(tutor.Email, student.StudentId);

        return (tutor, agreementId);
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
                InitialStartsAtUtc);

            dbContext.TutoringAgreements.Add(agreement);
            await dbContext.SaveChangesAsync();

            return agreement.Id.Value;
        });
    }

    private async Task<Guid> CreateLessonAsync(
        Guid agreementId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        LessonSeriesId? lessonSeriesId = null)
    {
        return await ExecuteDbAsync(async dbContext =>
        {
            var lesson = new Lesson(
                new TutoringAgreementId(agreementId),
                new TimeSlot(startsAtUtc, endsAtUtc),
                InitialStartsAtUtc,
                lessonSeriesId);

            dbContext.Lessons.Add(lesson);
            await dbContext.SaveChangesAsync();

            return lesson.Id.Value;
        });
    }

    private Task SetLessonStatusAsync(Guid lessonId, LessonStatus status)
    {
        return ExecuteDbAsync(dbContext =>
            dbContext.Lessons
                .Where(lesson => lesson.Id == new LessonId(lessonId))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(lesson => lesson.Status, status)));
    }

    private Task<LessonSnapshot> GetLessonAsync(Guid lessonId)
    {
        return ExecuteDbAsync(dbContext =>
            dbContext.Lessons
                .AsNoTracking()
                .Where(lesson => lesson.Id == new LessonId(lessonId))
                .Select(lesson => new LessonSnapshot(
                    lesson.Id.Value,
                    lesson.TutoringAgreementId.Value,
                    lesson.LessonSeriesId == null ? null : lesson.LessonSeriesId.Value.Value,
                    lesson.CreatedAtUtc,
                    lesson.Status,
                    lesson.TimeSlot.StartsAtUtc,
                    lesson.TimeSlot.EndsAtUtc))
                .SingleAsync());
    }

    private Task<HttpResponseMessage> RescheduleLessonAsync(
        string accessToken,
        Guid lessonId,
        DateTimeOffset startsAt,
        int durationMinutes)
    {
        return Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Put,
                $"{Endpoint}/{lessonId}/schedule",
                accessToken,
                new
                {
                    StartsAt = startsAt,
                    DurationMinutes = durationMinutes
                }));
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

    private sealed record LessonSnapshot(
        Guid Id,
        Guid TutoringAgreementId,
        Guid? LessonSeriesId,
        DateTimeOffset CreatedAtUtc,
        LessonStatus Status,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc);
}
