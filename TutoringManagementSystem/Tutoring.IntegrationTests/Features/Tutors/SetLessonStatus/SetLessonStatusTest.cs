using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.SetLessonStatus;

[Collection(IntegrationTestCollection.Name)]
public sealed class SetLessonStatusTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/me/lessons";

    private static readonly DateTimeOffset PastStartsAtUtc =
        new(2020, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset FutureStartsAtUtc =
        new(2099, 1, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SetLessonStatus_ToCompleted_ShouldCompleteLesson()
    {
        var (tutor, agreementId) =
            await CreateTutorAndAgreementAsync();

        var lessonId = await CreateLessonAsync(
            agreementId,
            PastStartsAtUtc,
            PastStartsAtUtc.AddHours(1));

        var response = await SetLessonStatusAsync(
            tutor.AccessToken,
            lessonId,
            LessonStatus.Completed);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        Assert.Equal(
            LessonStatus.Completed,
            await GetLessonStatusAsync(lessonId));
    }

    [Fact]
    public async Task SetLessonStatus_ToMissed_ShouldMarkLessonAsMissed()
    {
        var (tutor, agreementId) =
            await CreateTutorAndAgreementAsync();

        var lessonId = await CreateLessonAsync(
            agreementId,
            PastStartsAtUtc,
            PastStartsAtUtc.AddHours(1));

        var response = await SetLessonStatusAsync(
            tutor.AccessToken,
            lessonId,
            LessonStatus.Missed);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        Assert.Equal(
            LessonStatus.Missed,
            await GetLessonStatusAsync(lessonId));
    }

    [Theory]
    [InlineData(LessonStatus.Completed)]
    [InlineData(LessonStatus.Missed)]
    public async Task SetLessonStatus_BeforeLessonEnds_ShouldReturnBusinessRuleProblemAndNotModifyLesson(
        LessonStatus targetStatus)
    {
        var (tutor, agreementId) =
            await CreateTutorAndAgreementAsync();

        var lessonId = await CreateLessonAsync(
            agreementId,
            FutureStartsAtUtc,
            FutureStartsAtUtc.AddHours(1));

        var before =
            await GetLessonStatusAsync(lessonId);

        var response = await SetLessonStatusAsync(
            tutor.AccessToken,
            lessonId,
            targetStatus);

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            response.StatusCode);

        await AssertProblemCodeAsync(
            response,
            "Lessons.CannotBeFinalizedBeforeEnd");

        Assert.Equal(
            before,
            await GetLessonStatusAsync(lessonId));
    }

    [Theory]
    [InlineData(
        LessonStatus.Completed,
        LessonStatus.Completed)]
    [InlineData(
        LessonStatus.Completed,
        LessonStatus.Missed)]
    [InlineData(
        LessonStatus.Missed,
        LessonStatus.Missed)]
    [InlineData(
        LessonStatus.Missed,
        LessonStatus.Completed)]
    [InlineData(
        LessonStatus.Cancelled,
        LessonStatus.Completed)]
    [InlineData(
        LessonStatus.Cancelled,
        LessonStatus.Missed)]
    public async Task SetLessonStatus_WhenLessonIsAlreadyFinalized_ShouldReturnBusinessRuleProblemAndNotModifyLesson(
        LessonStatus currentStatus,
        LessonStatus targetStatus)
    {
        var (tutor, agreementId) =
            await CreateTutorAndAgreementAsync();

        var lessonId = await CreateLessonAsync(
            agreementId,
            PastStartsAtUtc,
            PastStartsAtUtc.AddHours(1));

        await SetLessonStatusInDatabaseAsync(
            lessonId,
            currentStatus);

        var before =
            await GetLessonStatusAsync(lessonId);

        var response = await SetLessonStatusAsync(
            tutor.AccessToken,
            lessonId,
            targetStatus);

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            response.StatusCode);

        await AssertProblemCodeAsync(
            response,
            "Lessons.CannotBeFinalized");

        Assert.Equal(
            before,
            await GetLessonStatusAsync(lessonId));
    }

    [Theory]
    [InlineData(LessonStatus.Scheduled)]
    [InlineData(LessonStatus.Cancelled)]
    public async Task SetLessonStatus_WithUnsupportedTargetStatus_ShouldReturnBadRequestAndNotModifyLesson(
        LessonStatus targetStatus)
    {
        var (tutor, agreementId) =
            await CreateTutorAndAgreementAsync();

        var lessonId = await CreateLessonAsync(
            agreementId,
            PastStartsAtUtc,
            PastStartsAtUtc.AddHours(1));

        var before =
            await GetLessonStatusAsync(lessonId);

        var response = await SetLessonStatusAsync(
            tutor.AccessToken,
            lessonId,
            targetStatus);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertProblemCodeAsync(
            response,
            "Request.Invalid");

        Assert.Equal(
            before,
            await GetLessonStatusAsync(lessonId));
    }

    [Fact]
    public async Task SetLessonStatus_WhenLessonDoesNotExist_ShouldReturnNotFoundProblem()
    {
        var tutor = await CreateTutorAsync(
            "tutor@test.pl",
            "tutor");

        var response = await SetLessonStatusAsync(
            tutor.AccessToken,
            Guid.NewGuid(),
            LessonStatus.Completed);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertProblemCodeAsync(
            response,
            "Lessons.LessonNotFound");
    }

    [Fact]
    public async Task SetLessonStatus_WhenLessonBelongsToAnotherTutor_ShouldReturnNotFoundAndNotModifyLesson()
    {
        var (_, agreementId) =
            await CreateTutorAndAgreementAsync();

        var otherTutor = await CreateTutorAsync(
            "other-tutor@test.pl",
            "other-tutor");

        var lessonId = await CreateLessonAsync(
            agreementId,
            PastStartsAtUtc,
            PastStartsAtUtc.AddHours(1));

        var before =
            await GetLessonStatusAsync(lessonId);

        var response = await SetLessonStatusAsync(
            otherTutor.AccessToken,
            lessonId,
            LessonStatus.Completed);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertProblemCodeAsync(
            response,
            "Lessons.LessonNotFound");

        Assert.Equal(
            before,
            await GetLessonStatusAsync(lessonId));
    }

    [Fact]
    public async Task SetLessonStatus_AsStudent_ShouldReturnForbiddenAndNotModifyLesson()
    {
        var (tutor, agreementId) =
            await CreateTutorAndAgreementAsync();

        var student = await CreateStudentAsync(
            "other-student@test.pl",
            "other-student");

        var lessonId = await CreateLessonAsync(
            agreementId,
            PastStartsAtUtc,
            PastStartsAtUtc.AddHours(1));

        var before =
            await GetLessonStatusAsync(lessonId);

        var response = await SetLessonStatusAsync(
            student.AccessToken,
            lessonId,
            LessonStatus.Completed);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            before,
            await GetLessonStatusAsync(lessonId));

        Assert.NotEqual(
            tutor.AccessToken,
            student.AccessToken);
    }

    [Fact]
    public async Task SetLessonStatus_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var response = await Client.PutAsJsonAsync(
            $"{Endpoint}/{Guid.NewGuid()}/status",
            new
            {
                Status = LessonStatus.Completed.ToString()
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
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

        var agreementId = await CreateAgreementAsync(
            tutor.Email,
            student.StudentId);

        return (tutor, agreementId);
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
            (await LoginAsync(
                email,
                Password))
            .Login.AccessToken;

        var studentId = await ExecuteDbAsync(
            dbContext =>
                dbContext.Students
                    .Where(student =>
                        student.Account != null &&
                        student.Account.Email.Value == email)
                    .Select(student =>
                        student.Id.Value)
                    .SingleAsync());

        return new TestStudent(
            accessToken,
            studentId);
    }

    private async Task<Guid> CreateAgreementAsync(
        string tutorEmail,
        Guid studentId)
    {
        return await ExecuteDbAsync(
            async dbContext =>
            {
                var tutorId = await dbContext.Tutors
                    .Where(tutor =>
                        tutor.Account.Email.Value ==
                        tutorEmail)
                    .Select(tutor =>
                        tutor.Id)
                    .SingleAsync();

                var agreement =
                    new TutoringAgreement(
                        tutorId,
                        new StudentId(studentId),
                        new Subject("Mathematics"),
                        new HourlyRate(
                            new Money(
                                100,
                                new Currency("PLN"))),
                        new AgreementTitle(
                            "Test Agreement"),
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
                var lesson = new Lesson(
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

    private Task SetLessonStatusInDatabaseAsync(
        Guid lessonId,
        LessonStatus status)
    {
        return ExecuteDbAsync(
            dbContext =>
                dbContext.Lessons
                    .Where(lesson =>
                        lesson.Id ==
                        new LessonId(lessonId))
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                lesson => lesson.Status,
                                status)));
    }

    private Task<LessonStatus> GetLessonStatusAsync(
        Guid lessonId)
    {
        return ExecuteDbAsync(
            dbContext =>
                dbContext.Lessons
                    .AsNoTracking()
                    .Where(lesson =>
                        lesson.Id ==
                        new LessonId(lessonId))
                    .Select(lesson =>
                        lesson.Status)
                    .SingleAsync());
    }

    private Task<HttpResponseMessage> SetLessonStatusAsync(
        string accessToken,
        Guid lessonId,
        LessonStatus status)
    {
        return Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Put,
                $"{Endpoint}/{lessonId}/status",
                accessToken,
                new
                {
                    Status = status.ToString()
                }));
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);

        Assert.True(
            problem!.Extensions.TryGetValue(
                "code",
                out var code));

        Assert.Equal(
            expectedCode,
            code?.ToString());
    }

    private sealed record TestTutor(
        string Email,
        string AccessToken);

    private sealed record TestStudent(
        string AccessToken,
        Guid StudentId);
}