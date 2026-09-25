using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.GetStudentLessonHistory;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.GetStudentLessonHistory;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetStudentLessonHistoryTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/students";
    private static readonly DateTimeOffset OlderPastStartsAtUtc =
        new(2020, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset NewerPastStartsAtUtc =
        new(2020, 2, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset FutureStartsAtUtc =
        new(2099, 1, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetStudentLessonHistory_WithPastLessons_ShouldReturnLessons()
    {
        var (tutor, student, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        var olderLessonId =
            await CreateLessonAsync(
                agreementId,
                OlderPastStartsAtUtc,
                OlderPastStartsAtUtc.AddHours(1));

        var newerLessonId =
            await CreateLessonAsync(
                agreementId,
                NewerPastStartsAtUtc,
                NewerPastStartsAtUtc.AddHours(1));

        var response =
            await GetStudentLessonHistoryAsync(
                tutor.AccessToken,
                student.StudentId);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var lessons =
            await response.Content
                .ReadFromJsonAsync<
                    List<StudentLessonHistoryResponse>>();

        Assert.NotNull(lessons);

        Assert.Equal(
            2,
            lessons.Count);

        Assert.Contains(
            lessons,
            lesson =>
                lesson.LessonId ==
                olderLessonId);

        Assert.Contains(
            lessons,
            lesson =>
                lesson.LessonId ==
                newerLessonId);
    }

    [Fact]
    public async Task GetStudentLessonHistory_ShouldReturnExpectedLessonData()
    {
        var (tutor, student, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        var lessonId =
            await CreateLessonAsync(
                agreementId,
                OlderPastStartsAtUtc,
                OlderPastStartsAtUtc.AddHours(1));

        var response =
            await GetStudentLessonHistoryAsync(
                tutor.AccessToken,
                student.StudentId);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var lessons =
            await response.Content
                .ReadFromJsonAsync<
                    List<StudentLessonHistoryResponse>>();

        Assert.NotNull(lessons);

        var lesson =
            Assert.Single(lessons);

        Assert.Equal(
            lessonId,
            lesson.LessonId);

        Assert.Equal(
            agreementId,
            lesson.TutoringAgreementId);

        Assert.Equal(
            OlderPastStartsAtUtc,
            lesson.StartsAtUtc);

        Assert.Equal(
            OlderPastStartsAtUtc.AddHours(1),
            lesson.EndsAtUtc);

        Assert.Equal(
            LessonStatus.Scheduled.ToString(),
            lesson.Status);

        Assert.Equal(
            "Mathematics",
            lesson.Subject);

        Assert.Equal(
            "Test Agreement",
            lesson.AgreementTitle);

        Assert.Null(
            lesson.CancellationReason);
    }

    [Fact]
    public async Task GetStudentLessonHistory_ShouldReturnLessonsNewestFirst()
    {
        var (tutor, student, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        var olderLessonId =
            await CreateLessonAsync(
                agreementId,
                OlderPastStartsAtUtc,
                OlderPastStartsAtUtc.AddHours(1));

        var newerLessonId =
            await CreateLessonAsync(
                agreementId,
                NewerPastStartsAtUtc,
                NewerPastStartsAtUtc.AddHours(1));

        var response =
            await GetStudentLessonHistoryAsync(
                tutor.AccessToken,
                student.StudentId);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var lessons =
            await response.Content
                .ReadFromJsonAsync<
                    List<StudentLessonHistoryResponse>>();

        Assert.NotNull(lessons);

        Assert.Equal(
            2,
            lessons.Count);

        Assert.Equal(
            newerLessonId,
            lessons[0].LessonId);

        Assert.Equal(
            olderLessonId,
            lessons[1].LessonId);
    }

    [Fact]
    public async Task GetStudentLessonHistory_WithFutureLesson_ShouldNotReturnFutureLesson()
    {
        var (tutor, student, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        var pastLessonId =
            await CreateLessonAsync(
                agreementId,
                OlderPastStartsAtUtc,
                OlderPastStartsAtUtc.AddHours(1));

        var futureLessonId =
            await CreateLessonAsync(
                agreementId,
                FutureStartsAtUtc,
                FutureStartsAtUtc.AddHours(1));

        var response =
            await GetStudentLessonHistoryAsync(
                tutor.AccessToken,
                student.StudentId);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var lessons =
            await response.Content
                .ReadFromJsonAsync<
                    List<StudentLessonHistoryResponse>>();

        Assert.NotNull(lessons);

        var lesson =
            Assert.Single(lessons);

        Assert.Equal(
            pastLessonId,
            lesson.LessonId);

        Assert.DoesNotContain(
            lessons,
            item =>
                item.LessonId ==
                futureLessonId);
    }

    [Fact]
    public async Task GetStudentLessonHistory_WithoutPastLessons_ShouldReturnEmptyList()
    {
        var (tutor, student, _) =
            await CreateTutorStudentAndAgreementAsync();

        var response =
            await GetStudentLessonHistoryAsync(
                tutor.AccessToken,
                student.StudentId);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var lessons =
            await response.Content
                .ReadFromJsonAsync<
                    List<StudentLessonHistoryResponse>>();

        Assert.NotNull(lessons);

        Assert.Empty(
            lessons);
    }

    [Fact]
    public async Task GetStudentLessonHistory_WhenStudentBelongsToAnotherTutor_ShouldReturnNotFound()
    {
        var (_, student, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        await CreateLessonAsync(
            agreementId,
            OlderPastStartsAtUtc,
            OlderPastStartsAtUtc.AddHours(1));

        var otherTutor =
            await CreateTutorAsync(
                "other-tutor@test.pl",
                "other-tutor");

        var response =
            await GetStudentLessonHistoryAsync(
                otherTutor.AccessToken,
                student.StudentId);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetStudentLessonHistory_WhenStudentDoesNotExist_ShouldReturnNotFound()
    {
        var tutor =
            await CreateTutorAsync(
                "tutor@test.pl",
                "tutor");

        var response =
            await GetStudentLessonHistoryAsync(
                tutor.AccessToken,
                Guid.NewGuid());

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetStudentLessonHistory_AsStudent_ShouldReturnForbidden()
    {
        var (_, student, _) =
            await CreateTutorStudentAndAgreementAsync();

        var otherStudent =
            await CreateStudentAsync(
                "other-student@test.pl",
                "other-student");

        var response =
            await GetStudentLessonHistoryAsync(
                otherStudent.AccessToken,
                student.StudentId);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task GetStudentLessonHistory_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var response =
            await Client.GetAsync(
                $"{Endpoint}/{Guid.NewGuid()}/lessons/history");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetStudentLessonHistory_WithEndedAgreement_ShouldReturnHistory()
    {
        var (tutor, student, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        var lessonId =
            await CreateLessonAsync(
                agreementId,
                OlderPastStartsAtUtc,
                OlderPastStartsAtUtc.AddHours(1));

        await EndAgreementAsync(
            agreementId);

        var response =
            await GetStudentLessonHistoryAsync(
                tutor.AccessToken,
                student.StudentId);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var lessons =
            await response.Content
                .ReadFromJsonAsync<
                    List<StudentLessonHistoryResponse>>();

        Assert.NotNull(lessons);

        var lesson =
            Assert.Single(lessons);

        Assert.Equal(
            lessonId,
            lesson.LessonId);
    }

    [Fact]
    public async Task GetStudentLessonHistory_WithMultipleAgreements_ShouldReturnLessonsFromAllAgreements()
    {
        var tutor =
            await CreateTutorAsync(
                "tutor@test.pl",
                "tutor");

        var student =
            await CreateStudentAsync(
                "student@test.pl",
                "student");

        var firstAgreementId =
            await CreateAgreementAsync(
                tutor.Email,
                student.StudentId,
                "First Agreement");

        var secondAgreementId =
            await CreateAgreementAsync(
                tutor.Email,
                student.StudentId,
                "Second Agreement");

        var firstLessonId =
            await CreateLessonAsync(
                firstAgreementId,
                OlderPastStartsAtUtc,
                OlderPastStartsAtUtc.AddHours(1));

        var secondLessonId =
            await CreateLessonAsync(
                secondAgreementId,
                NewerPastStartsAtUtc,
                NewerPastStartsAtUtc.AddHours(1));

        var response =
            await GetStudentLessonHistoryAsync(
                tutor.AccessToken,
                student.StudentId);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var lessons =
            await response.Content
                .ReadFromJsonAsync<
                    List<StudentLessonHistoryResponse>>();

        Assert.NotNull(lessons);

        Assert.Equal(
            2,
            lessons.Count);

        Assert.Contains(
            lessons,
            lesson =>
                lesson.LessonId ==
                    firstLessonId &&
                lesson.TutoringAgreementId ==
                    firstAgreementId &&
                lesson.AgreementTitle ==
                    "First Agreement");

        Assert.Contains(
            lessons,
            lesson =>
                lesson.LessonId ==
                    secondLessonId &&
                lesson.TutoringAgreementId ==
                    secondAgreementId &&
                lesson.AgreementTitle ==
                    "Second Agreement");
    }

    private async Task<(
        TestTutor Tutor,
        TestStudent Student,
        Guid AgreementId)>
        CreateTutorStudentAndAgreementAsync()
    {
        var tutor =
            await CreateTutorAsync(
                "tutor@test.pl",
                "tutor");

        var student =
            await CreateStudentAsync(
                "student@test.pl",
                "student");

        var agreementId =
            await CreateAgreementAsync(
                tutor.Email,
                student.StudentId);

        return (
            tutor,
            student,
            agreementId);
    }

    private async Task<TestTutor> CreateTutorAsync(
        string email,
        string userName)
    {
        var response =
            await Client.PostAsJsonAsync(
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
            await GetVerificationTokenAsync(
                email));

        var accessToken =
            (await LoginAsync(
                email,
                Password))
            .Login.AccessToken;

        return new TestTutor(
            email,
            accessToken);
    }

    private async Task<TestStudent> CreateStudentAsync(
        string email,
        string userName)
    {
        var response =
            await Client.PostAsJsonAsync(
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
            await GetVerificationTokenAsync(
                email));

        var accessToken =
            (await LoginAsync(
                email,
                Password))
            .Login.AccessToken;

        var studentId =
            await ExecuteDbAsync(
                dbContext =>
                    dbContext.Students
                        .Where(student =>
                            student.Account != null &&
                            student.Account.Email.Value ==
                                email)
                        .Select(student =>
                            student.Id.Value)
                        .SingleAsync());

        return new TestStudent(
            accessToken,
            studentId);
    }

    private Task<Guid> CreateAgreementAsync(
        string tutorEmail,
        Guid studentId)
    {
        return CreateAgreementAsync(
            tutorEmail,
            studentId,
            "Test Agreement");
    }

    private async Task<Guid> CreateAgreementAsync(
        string tutorEmail,
        Guid studentId,
        string title)
    {
        return await ExecuteDbAsync(
            async dbContext =>
            {
                var tutorId =
                    await dbContext.Tutors
                        .Where(tutor =>
                            tutor.Account.Email.Value ==
                                tutorEmail)
                        .Select(tutor =>
                            tutor.Id)
                        .SingleAsync();

                var agreement =
                    new TutoringAgreement(
                        tutorId,
                        new StudentId(
                            studentId),
                        new Subject(
                            "Mathematics"),
                        new HourlyRate(
                            new Money(
                                100,
                                new Currency(
                                    "PLN"))),
                        new AgreementTitle(
                            title),
                        DateTimeOffset.UtcNow);

                dbContext.TutoringAgreements.Add(
                    agreement);

                await dbContext.SaveChangesAsync();

                return agreement.Id.Value;
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
                var lesson =
                    new Lesson(
                        new TutoringAgreementId(
                            agreementId),
                        new TimeSlot(
                            startsAtUtc,
                            endsAtUtc),
                        DateTimeOffset.UtcNow);

                dbContext.Lessons.Add(
                    lesson);

                await dbContext.SaveChangesAsync();

                return lesson.Id.Value;
            });
    }

    private Task EndAgreementAsync(
        Guid agreementId)
    {
        return ExecuteDbAsync(
            async dbContext =>
            {
                var agreement =
                    await dbContext
                        .TutoringAgreements
                        .SingleAsync(
                            agreement =>
                                agreement.Id ==
                                new TutoringAgreementId(
                                    agreementId));

                dbContext.Entry(agreement)
                    .Property(item =>
                        item.Status)
                    .CurrentValue =
                        AgreementStatus.Ended;

                await dbContext
                    .SaveChangesAsync();
            });
    }

    private Task<HttpResponseMessage> GetStudentLessonHistoryAsync(
        string accessToken,
        Guid studentId)
    {
        return Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"{Endpoint}/{studentId}/lessons/history",
                accessToken));
    }

    private sealed record TestTutor(
        string Email,
        string AccessToken);

    private sealed record TestStudent(
        string AccessToken,
        Guid StudentId);
}