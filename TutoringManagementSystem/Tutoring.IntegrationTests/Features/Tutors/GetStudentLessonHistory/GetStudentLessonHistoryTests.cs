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

    [Fact]
    public async Task GetStudentLessonHistory_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var response = await Client.GetAsync(
            $"/api/tutors/students/{Guid.NewGuid()}/lessons/history");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentLessonHistory_AsStudent_ShouldReturnForbidden()
    {
        var student = await CreateStudentAsync(
            "student@test.pl",
            "student");

        var response = await GetStudentLessonHistoryAsync(
            student.AccessToken,
            student.StudentId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentLessonHistory_ShouldReturnPastLessonsNewestFirst()
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
        var now = DateTimeOffset.UtcNow;
        var oldestLessonId = await CreateLessonAsync(
            agreement.Id.Value,
            now.AddDays(-2),
            now.AddDays(-2).AddHours(1));
        var newestLessonId = await CreateLessonAsync(
            agreement.Id.Value,
            now.AddDays(-1),
            now.AddDays(-1).AddHours(1));
        await CreateLessonAsync(
            agreement.Id.Value,
            now.AddDays(1),
            now.AddDays(1).AddHours(1));

        var response = await GetStudentLessonHistoryAsync(
            tutor.AccessToken,
            student.StudentId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lessons = await response.Content
            .ReadFromJsonAsync<List<StudentLessonHistoryResponse>>();

        Assert.NotNull(lessons);
        Assert.Equal(
            [newestLessonId, oldestLessonId],
            lessons!.Select(lesson => lesson.LessonId));
        Assert.All(lessons, lesson =>
        {
            Assert.Equal(agreement.Id.Value, lesson.TutoringAgreementId);
            Assert.Equal("Test Agreement", lesson.Title);
            Assert.Equal("Mathematics", lesson.Subject);
            Assert.Equal(LessonStatus.Scheduled.ToString(), lesson.Status);
        });
    }

    [Fact]
    public async Task GetStudentLessonHistory_WithEndedAgreementAndNoLessons_ShouldReturnEmptyList()
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

        await ExecuteDbAsync(async dbContext =>
        {
            var storedAgreement = await dbContext.TutoringAgreements
                .SingleAsync(item => item.Id == agreement.Id);
            dbContext.Entry(storedAgreement).Property(item => item.Status)
                .CurrentValue = AgreementStatus.Ended;
            await dbContext.SaveChangesAsync();
        });

        var response = await GetStudentLessonHistoryAsync(
            tutor.AccessToken,
            student.StudentId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lessons = await response.Content
            .ReadFromJsonAsync<List<StudentLessonHistoryResponse>>();

        Assert.NotNull(lessons);
        Assert.Empty(lessons!);
    }

    [Fact]
    public async Task GetStudentLessonHistory_ForAnotherTutorsStudent_ShouldReturnNotFound()
    {
        var requestingTutor = await CreateTutorAsync(
            "requesting-tutor@test.pl",
            "requesting-tutor");
        var owningTutor = await CreateTutorAsync(
            "owning-tutor@test.pl",
            "owning-tutor");
        var student = await CreateStudentAsync(
            "student@test.pl",
            "student");
        await CreateAgreementAsync(owningTutor.Email, student.StudentId);

        var response = await GetStudentLessonHistoryAsync(
            requestingTutor.AccessToken,
            student.StudentId);

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
        return new TestTutor(email, accessToken);
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

        return new TestStudent(accessToken, studentId);
    }

    private async Task<TutoringAgreement> CreateAgreementAsync(
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
        return await ExecuteDbAsync(async dbContext =>
        {
            var lesson = new Lesson(
                new TutoringAgreementId(agreementId),
                new TimeSlot(startsAtUtc, endsAtUtc),
                DateTimeOffset.UtcNow);
            dbContext.Lessons.Add(lesson);
            await dbContext.SaveChangesAsync();

            return lesson.Id.Value;
        });
    }

    private Task<HttpResponseMessage> GetStudentLessonHistoryAsync(
        string accessToken,
        Guid studentId)
    {
        return Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tutors/students/{studentId}/lessons/history",
                accessToken));
    }

    private sealed record TestTutor(
        string Email,
        string AccessToken);

    private sealed record TestStudent(
        string AccessToken,
        Guid StudentId);
}
