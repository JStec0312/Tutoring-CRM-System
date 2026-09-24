using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Calendar.GetCalendarLessons;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Calendar.GetCalendarLessons;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetCalendarLessonsTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/calendar";

    private static readonly DateTimeOffset ReferenceDate =
        new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetCalendarLessons_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var response = await Client.GetAsync(
            BuildEndpoint(
                ReferenceDate,
                ReferenceDate.AddDays(1)));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCalendarLessons_WithInvalidRange_ShouldReturnBadRequest()
    {
        var tutor = await CreateTutorAsync(
            "tutor@test.pl",
            "tutor");

        var response = await GetCalendarLessonsAsync(
            tutor.AccessToken,
            ReferenceDate,
            ReferenceDate);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCalendarLessons_AsTutor_ShouldReturnOwnLessons()
    {
        var tutor = await CreateTutorAsync(
            "tutor@test.pl",
            "tutor");

        var student = await CreateStudentAsync(
            "student@test.pl",
            "student");

        var agreement = await CreateAgreementAsync(
            tutor.Email,
            student.StudentId);

        var lessonId = await CreateLessonAsync(
            agreement.Id.Value,
            ReferenceDate,
            ReferenceDate.AddHours(1));

        var response = await GetCalendarLessonsAsync(
            tutor.AccessToken,
            ReferenceDate.AddHours(-1),
            ReferenceDate.AddHours(2));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var lessons = await response.Content
            .ReadFromJsonAsync<List<CalendarLessonResponse>>();

        var lesson = Assert.Single(lessons!);

        Assert.Equal(lessonId, lesson.LessonId);
        Assert.Equal(agreement.Id.Value, lesson.TutoringAgreementId);
        Assert.Equal("Test Agreement", lesson.Title);
        Assert.Equal("Mathematics", lesson.Subject);
        Assert.Equal(ReferenceDate, lesson.StartsAtUtc);
        Assert.Equal(ReferenceDate.AddHours(1), lesson.EndsAtUtc);
        Assert.Equal(LessonStatus.Scheduled.ToString(), lesson.Status);
        Assert.Equal(student.StudentId, lesson.StudentId);
        Assert.Equal("student", lesson.StudentDisplayName);
        Assert.Equal(tutor.TutorId, lesson.TutorId);
        Assert.Equal("Tutor", lesson.TutorFirstName);
        Assert.Equal("Test", lesson.TutorLastName);
    }

    [Fact]
    public async Task GetCalendarLessons_AsStudent_ShouldReturnAssignedLessons()
    {
        var tutor = await CreateTutorAsync(
            "tutor@test.pl",
            "tutor");

        var student = await CreateStudentAsync(
            "student@test.pl",
            "student");

        var agreement = await CreateAgreementAsync(
            tutor.Email,
            student.StudentId);

        var lessonId = await CreateLessonAsync(
            agreement.Id.Value,
            ReferenceDate,
            ReferenceDate.AddHours(1));

        var response = await GetCalendarLessonsAsync(
            student.AccessToken,
            ReferenceDate.AddHours(-1),
            ReferenceDate.AddHours(2));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var lessons = await response.Content
            .ReadFromJsonAsync<List<CalendarLessonResponse>>();

        var lesson = Assert.Single(lessons!);

        Assert.Equal(
            lessonId,
            lesson.LessonId);
    }

    [Fact]
    public async Task GetCalendarLessons_ShouldNotReturnAnotherUsersLessons()
    {
        var tutorA = await CreateTutorAsync(
            "tutor-a@test.pl",
            "tutor-a");

        var tutorB = await CreateTutorAsync(
            "tutor-b@test.pl",
            "tutor-b");

        var studentA = await CreateStudentAsync(
            "student-a@test.pl",
            "student-a");

        var studentB = await CreateStudentAsync(
            "student-b@test.pl",
            "student-b");

        var agreementA = await CreateAgreementAsync(
            tutorA.Email,
            studentA.StudentId);

        var agreementB = await CreateAgreementAsync(
            tutorB.Email,
            studentB.StudentId);

        var expectedLessonId = await CreateLessonAsync(
            agreementA.Id.Value,
            ReferenceDate,
            ReferenceDate.AddHours(1));

        await CreateLessonAsync(
            agreementB.Id.Value,
            ReferenceDate,
            ReferenceDate.AddHours(1));

        var response = await GetCalendarLessonsAsync(
            tutorA.AccessToken,
            ReferenceDate.AddHours(-1),
            ReferenceDate.AddHours(2));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var lessons = await response.Content
            .ReadFromJsonAsync<List<CalendarLessonResponse>>();

        var lesson = Assert.Single(lessons!);

        Assert.Equal(
            expectedLessonId,
            lesson.LessonId);
    }

    [Fact]
    public async Task GetCalendarLessons_WhenLessonOverlapsBeginningOfRange_ShouldReturnLesson()
    {
        var (tutor, agreementId) =
            await CreateTutorAndAgreementAsync();

        var lessonId = await CreateLessonAsync(
            agreementId,
            ReferenceDate.AddMinutes(-30),
            ReferenceDate.AddMinutes(30));

        var response = await GetCalendarLessonsAsync(
            tutor.AccessToken,
            ReferenceDate,
            ReferenceDate.AddHours(1));

        var lessons = await response.Content
            .ReadFromJsonAsync<List<CalendarLessonResponse>>();

        var lesson = Assert.Single(lessons!);

        Assert.Equal(
            lessonId,
            lesson.LessonId);
    }

    [Fact]
    public async Task GetCalendarLessons_WhenLessonOverlapsEndOfRange_ShouldReturnLesson()
    {
        var (tutor, agreementId) =
            await CreateTutorAndAgreementAsync();

        var lessonId = await CreateLessonAsync(
            agreementId,
            ReferenceDate.AddMinutes(30),
            ReferenceDate.AddHours(1).AddMinutes(30));

        var response = await GetCalendarLessonsAsync(
            tutor.AccessToken,
            ReferenceDate,
            ReferenceDate.AddHours(1));

        var lessons = await response.Content
            .ReadFromJsonAsync<List<CalendarLessonResponse>>();

        var lesson = Assert.Single(lessons!);

        Assert.Equal(
            lessonId,
            lesson.LessonId);
    }

    [Fact]
    public async Task GetCalendarLessons_WhenLessonEndsExactlyAtRangeStart_ShouldNotReturnLesson()
    {
        var (tutor, agreementId) =
            await CreateTutorAndAgreementAsync();

        await CreateLessonAsync(
            agreementId,
            ReferenceDate.AddHours(-1),
            ReferenceDate);

        var response = await GetCalendarLessonsAsync(
            tutor.AccessToken,
            ReferenceDate,
            ReferenceDate.AddHours(1));

        var lessons = await response.Content
            .ReadFromJsonAsync<List<CalendarLessonResponse>>();

        Assert.Empty(lessons!);
    }

    [Fact]
    public async Task GetCalendarLessons_WhenLessonStartsExactlyAtRangeEnd_ShouldNotReturnLesson()
    {
        var (tutor, agreementId) =
            await CreateTutorAndAgreementAsync();

        await CreateLessonAsync(
            agreementId,
            ReferenceDate.AddHours(1),
            ReferenceDate.AddHours(2));

        var response = await GetCalendarLessonsAsync(
            tutor.AccessToken,
            ReferenceDate,
            ReferenceDate.AddHours(1));

        var lessons = await response.Content
            .ReadFromJsonAsync<List<CalendarLessonResponse>>();

        Assert.Empty(lessons!);
    }

    [Fact]
    public async Task GetCalendarLessons_ShouldReturnLessonsOrderedByStartTime()
    {
        var (tutor, agreementId) =
            await CreateTutorAndAgreementAsync();

        var lastLessonId = await CreateLessonAsync(
            agreementId,
            ReferenceDate.AddHours(3),
            ReferenceDate.AddHours(4));

        var firstLessonId = await CreateLessonAsync(
            agreementId,
            ReferenceDate.AddHours(1),
            ReferenceDate.AddHours(2));

        var middleLessonId = await CreateLessonAsync(
            agreementId,
            ReferenceDate.AddHours(2),
            ReferenceDate.AddHours(3));

        var response = await GetCalendarLessonsAsync(
            tutor.AccessToken,
            ReferenceDate,
            ReferenceDate.AddHours(5));

        var lessons = await response.Content
            .ReadFromJsonAsync<List<CalendarLessonResponse>>();

        Assert.Equal(
            new[]
            {
                firstLessonId,
                middleLessonId,
                lastLessonId
            },
            lessons!.Select(lesson => lesson.LessonId));
    }

    [Fact]
    public async Task GetCalendarLessons_ForDayRange_ShouldReturnOnlyLessonsFromDay()
    {
        var (tutor, agreementId) =
            await CreateTutorAndAgreementAsync();

        var dayStart = new DateTimeOffset(
            2026, 9, 24, 0, 0, 0, TimeSpan.Zero);

        var lessonId = await CreateLessonAsync(
            agreementId,
            dayStart.AddHours(10),
            dayStart.AddHours(11));

        await CreateLessonAsync(
            agreementId,
            dayStart.AddDays(1).AddHours(10),
            dayStart.AddDays(1).AddHours(11));

        var response = await GetCalendarLessonsAsync(
            tutor.AccessToken,
            dayStart,
            dayStart.AddDays(1));

        var lessons = await response.Content
            .ReadFromJsonAsync<List<CalendarLessonResponse>>();

        var lesson = Assert.Single(lessons!);

        Assert.Equal(
            lessonId,
            lesson.LessonId);
    }

    [Fact]
    public async Task GetCalendarLessons_ForWeekRange_ShouldReturnLessonsFromWeek()
    {
        var (tutor, agreementId) =
            await CreateTutorAndAgreementAsync();

        var weekStart = new DateTimeOffset(
            2026, 9, 21, 0, 0, 0, TimeSpan.Zero);

        var mondayLesson = await CreateLessonAsync(
            agreementId,
            weekStart.AddHours(10),
            weekStart.AddHours(11));

        var sundayLesson = await CreateLessonAsync(
            agreementId,
            weekStart.AddDays(6).AddHours(10),
            weekStart.AddDays(6).AddHours(11));

        await CreateLessonAsync(
            agreementId,
            weekStart.AddDays(7).AddHours(10),
            weekStart.AddDays(7).AddHours(11));

        var response = await GetCalendarLessonsAsync(
            tutor.AccessToken,
            weekStart,
            weekStart.AddDays(7));

        var lessons = await response.Content
            .ReadFromJsonAsync<List<CalendarLessonResponse>>();

        Assert.Equal(
            new[]
            {
                mondayLesson,
                sundayLesson
            },
            lessons!.Select(lesson => lesson.LessonId));
    }

    [Fact]
    public async Task GetCalendarLessons_ForMonthRange_ShouldReturnLessonsFromMonth()
    {
        var (tutor, agreementId) =
            await CreateTutorAndAgreementAsync();

        var monthStart = new DateTimeOffset(
            2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

        var firstLesson = await CreateLessonAsync(
            agreementId,
            monthStart.AddDays(2),
            monthStart.AddDays(2).AddHours(1));

        var lastLesson = await CreateLessonAsync(
            agreementId,
            monthStart.AddDays(28),
            monthStart.AddDays(28).AddHours(1));

        await CreateLessonAsync(
            agreementId,
            monthStart.AddMonths(1),
            monthStart.AddMonths(1).AddHours(1));

        var response = await GetCalendarLessonsAsync(
            tutor.AccessToken,
            monthStart,
            monthStart.AddMonths(1));

        var lessons = await response.Content
            .ReadFromJsonAsync<List<CalendarLessonResponse>>();

        Assert.Equal(
            new[]
            {
                firstLesson,
                lastLesson
            },
            lessons!.Select(lesson => lesson.LessonId));
    }

    private async Task<(TestTutor Tutor, Guid AgreementId)>
        CreateTutorAndAgreementAsync()
    {
        var tutor = await CreateTutorAsync(
            "tutor@test.pl",
            "tutor");

        var student = await CreateStudentAsync(
            "student@test.pl",
            "student");

        var agreement = await CreateAgreementAsync(
            tutor.Email,
            student.StudentId);

        return (
            tutor,
            agreement.Id.Value);
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

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        await ConfirmEmailAsync(
            await GetVerificationTokenAsync(email));

        var accessToken =
            (await LoginAsync(email, Password))
            .Login.AccessToken;

        var tutorId = await ExecuteDbAsync(
            dbContext =>
                dbContext.Tutors
                    .Where(tutor =>
                        tutor.Account.Email.Value == email)
                    .Select(tutor => tutor.Id.Value)
                    .SingleAsync());

        return new TestTutor(
            email,
            accessToken,
            tutorId);
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

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        await ConfirmEmailAsync(
            await GetVerificationTokenAsync(email));

        var accessToken =
            (await LoginAsync(email, Password))
            .Login.AccessToken;

        var studentId = await ExecuteDbAsync(
            dbContext =>
                dbContext.Students
                    .Where(student =>
                        student.Account != null &&
                        student.Account.Email.Value == email)
                    .Select(student => student.Id.Value)
                    .SingleAsync());

        return new TestStudent(
            accessToken,
            studentId);
    }

    private async Task<TutoringAgreement> CreateAgreementAsync(
        string tutorEmail,
        Guid studentId)
    {
        return await ExecuteDbAsync(
            async dbContext =>
            {
                var tutorId = await dbContext.Tutors
                    .Where(tutor =>
                        tutor.Account.Email.Value == tutorEmail)
                    .Select(tutor => tutor.Id)
                    .SingleAsync();

                var agreement = new TutoringAgreement(
                    tutorId,
                    new StudentId(studentId),
                    new Subject("Mathematics"),
                    new HourlyRate(
                        new Money(
                            100,
                            new Currency("PLN"))),
                    new AgreementTitle("Test Agreement"),
                    DateTimeOffset.UtcNow);

                dbContext.TutoringAgreements.Add(agreement);

                await dbContext.SaveChangesAsync();

                return agreement;
            });
    }

    private async Task<Guid> CreateLessonAsync(
        Guid agreementId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc)
    {
        return await ExecuteDbAsync(
            async dbContext =>
            {
                var lesson = new Lesson(
                    new TutoringAgreementId(agreementId),
                    new TimeSlot(
                        startsAtUtc,
                        endsAtUtc),
                    DateTimeOffset.UtcNow);

                dbContext.Lessons.Add(lesson);

                await dbContext.SaveChangesAsync();

                return lesson.Id.Value;
            });
    }

    private Task<HttpResponseMessage> GetCalendarLessonsAsync(
        string accessToken,
        DateTimeOffset from,
        DateTimeOffset to)
    {
        return Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Get,
                BuildEndpoint(from, to),
                accessToken));
    }

    private static string BuildEndpoint(
        DateTimeOffset from,
        DateTimeOffset to)
    {
        return
            $"{Endpoint}" +
            $"?from={Uri.EscapeDataString(from.ToString("O"))}" +
            $"&to={Uri.EscapeDataString(to.ToString("O"))}";
    }

    private sealed record TestTutor(
        string Email,
        string AccessToken,
        Guid TutorId);

    private sealed record TestStudent(
        string AccessToken,
        Guid StudentId);
}