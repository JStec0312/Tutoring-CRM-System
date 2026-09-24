using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Lessons.CancelLesson;

[Collection(IntegrationTestCollection.Name)]
public sealed class CancelLessonTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/lessons";

    private static readonly DateTimeOffset StartsAtUtc =
        new(2099, 9, 25, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CancelLesson_AsTutor_ShouldCancelLesson()
    {
        var (tutor, student, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        var lessonId = await CreateLessonAsync(agreementId);

        var response = await CancelLessonAsync(
            tutor.AccessToken,
            lessonId,
            LessonCancellationParty.Tutor,
            "Tutor cannot attend");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var lesson = await GetLessonAsync(lessonId);

        Assert.Equal(
            LessonStatus.Cancelled,
            lesson.Status);

        Assert.Equal(
            LessonCancellationParty.Tutor,
            lesson.LessonCancellationParty);

        Assert.Equal(
            "Tutor cannot attend",
            lesson.CancellationReason);

        Assert.Equal(
            tutor.UserAccountId,
            lesson.CancelledByUserAccountId);

        Assert.NotNull(lesson.CancelledAtUtc);

        Assert.NotEqual(
            student.UserAccountId,
            lesson.CancelledByUserAccountId);
    }

    [Fact]
    public async Task CancelLesson_AsStudent_ShouldCancelLessonAsStudent()
    {
        var (_, student, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        var lessonId = await CreateLessonAsync(agreementId);

        var response = await CancelLessonAsync(
            student.AccessToken,
            lessonId,
            null,
            "Student cannot attend");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var lesson = await GetLessonAsync(lessonId);

        Assert.Equal(
            LessonStatus.Cancelled,
            lesson.Status);

        Assert.Equal(
            LessonCancellationParty.Student,
            lesson.LessonCancellationParty);

        Assert.Equal(
            "Student cannot attend",
            lesson.CancellationReason);

        Assert.Equal(
            student.UserAccountId,
            lesson.CancelledByUserAccountId);

        Assert.NotNull(lesson.CancelledAtUtc);
    }

    [Fact]
    public async Task CancelLesson_AsTutorForManagedStudent_ShouldRecordStudentAsCancellationParty()
    {
        var tutor =
            await CreateTutorAsync(
                "tutor@test.pl",
                "tutor");

        var studentId =
            await CreateManagedStudentAsync(
                "Jasio Kowalski");

        var agreementId =
            await CreateAgreementAsync(
                tutor.Email,
                studentId);

        var lessonId =
            await CreateLessonAsync(
                agreementId);

        var response = await CancelLessonAsync(
            tutor.AccessToken,
            lessonId,
            LessonCancellationParty.Student,
            null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var lesson =
            await GetLessonAsync(
                lessonId);

        Assert.Equal(
            LessonStatus.Cancelled,
            lesson.Status);

        Assert.Equal(
            LessonCancellationParty.Student,
            lesson.LessonCancellationParty);

        Assert.Null(
            lesson.CancellationReason);

        Assert.Equal(
            tutor.UserAccountId,
            lesson.CancelledByUserAccountId);

        Assert.NotNull(
            lesson.CancelledAtUtc);
    }

    [Fact]
    public async Task CancelLesson_WithoutReason_ShouldCancelLessonWithNullReason()
    {
        var (tutor, _, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        var lessonId =
            await CreateLessonAsync(
                agreementId);

        var response = await CancelLessonAsync(
            tutor.AccessToken,
            lessonId,
            LessonCancellationParty.Tutor,
            null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var lesson =
            await GetLessonAsync(
                lessonId);

        Assert.Equal(
            LessonStatus.Cancelled,
            lesson.Status);

        Assert.Null(
            lesson.CancellationReason);
    }

    [Fact]
    public async Task CancelLesson_WithWhitespaceReason_ShouldStoreNullReason()
    {
        var (tutor, _, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        var lessonId =
            await CreateLessonAsync(
                agreementId);

        var response = await CancelLessonAsync(
            tutor.AccessToken,
            lessonId,
            LessonCancellationParty.Tutor,
            "   ");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var lesson =
            await GetLessonAsync(
                lessonId);

        Assert.Null(
            lesson.CancellationReason);
    }

    [Fact]
    public async Task CancelLesson_WhenLessonDoesNotExist_ShouldReturnNotFoundProblem()
    {
        var tutor =
            await CreateTutorAsync(
                "tutor@test.pl",
                "tutor");

        var response = await CancelLessonAsync(
            tutor.AccessToken,
            Guid.NewGuid(),
            LessonCancellationParty.Tutor,
            null);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertProblemCodeAsync(
            response,
            "Lessons.NotFoundForUser");
    }

    [Fact]
    public async Task CancelLesson_WhenLessonBelongsToAnotherTutor_ShouldReturnNotFoundAndNotModifyLesson()
    {
        var (owner, _, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        var otherTutor =
            await CreateTutorAsync(
                "other-tutor@test.pl",
                "other-tutor");

        var lessonId =
            await CreateLessonAsync(
                agreementId);

        var before =
            await GetLessonAsync(
                lessonId);

        var response = await CancelLessonAsync(
            otherTutor.AccessToken,
            lessonId,
            LessonCancellationParty.Tutor,
            null);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertProblemCodeAsync(
            response,
            "Lessons.NotFoundForUser");

        Assert.Equal(
            before,
            await GetLessonAsync(lessonId));

        Assert.NotEqual(
            owner.AccessToken,
            otherTutor.AccessToken);
    }

    [Fact]
    public async Task CancelLesson_WhenLessonBelongsToAnotherStudent_ShouldReturnNotFoundAndNotModifyLesson()
    {
        var (_, _, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        var otherStudent =
            await CreateStudentAsync(
                "other-student@test.pl",
                "other-student");

        var lessonId =
            await CreateLessonAsync(
                agreementId);

        var before =
            await GetLessonAsync(
                lessonId);

        var response = await CancelLessonAsync(
            otherStudent.AccessToken,
            lessonId,
            null,
            null);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertProblemCodeAsync(
            response,
            "Lessons.NotFoundForUser");

        Assert.Equal(
            before,
            await GetLessonAsync(lessonId));
    }

    [Fact]
    public async Task CancelLesson_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            $"{Endpoint}/{Guid.NewGuid()}/cancel",
            new
            {
                CancellationParty =
                    LessonCancellationParty.Tutor,

                Reason = (string?)null
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

   [Fact]
    public async Task CancelLesson_AsStudentWithTutorCancellationParty_ShouldReturnUnauthorizedAndNotModifyLesson()
    {
        var (_, student, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        var lessonId =
            await CreateLessonAsync(
                agreementId);

        var before =
            await GetLessonAsync(
                lessonId);

        var response = await CancelLessonAsync(
            student.AccessToken,
            lessonId,
            LessonCancellationParty.Tutor,
            null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        await AssertProblemCodeAsync(
            response,
            "Lessons.CanNotCancelAsTutorBeingStudent");

        Assert.Equal(
            before,
            await GetLessonAsync(lessonId));
    }

    [Theory]
    [InlineData(LessonStatus.Completed)]
    [InlineData(LessonStatus.Cancelled)]
    [InlineData(LessonStatus.Missed)]
    public async Task CancelLesson_WhenLessonIsNotScheduled_ShouldReturnBusinessRuleProblemAndNotModifyLesson(
        LessonStatus status)
    {
        var (tutor, _, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        var lessonId =
            await CreateLessonAsync(
                agreementId);

        await SetLessonStatusAsync(
            lessonId,
            status);

        var before =
            await GetLessonAsync(
                lessonId);

        var response = await CancelLessonAsync(
            tutor.AccessToken,
            lessonId,
            LessonCancellationParty.Tutor,
            null);

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            response.StatusCode);

        await AssertProblemCodeAsync(
            response,
            "Lessons.CanNotBeCancelled");

        Assert.Equal(
            before,
            await GetLessonAsync(lessonId));
    }

    [Fact]
    public async Task CancelLesson_WithReasonLongerThan500Characters_ShouldReturnBadRequestAndNotModifyLesson()
    {
        var (tutor, _, agreementId) =
            await CreateTutorStudentAndAgreementAsync();

        var lessonId =
            await CreateLessonAsync(
                agreementId);

        var before =
            await GetLessonAsync(
                lessonId);

        var response = await CancelLessonAsync(
            tutor.AccessToken,
            lessonId,
            LessonCancellationParty.Tutor,
            new string('A', 501));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertProblemCodeAsync(
            response,
            "Request.Invalid");

        Assert.Equal(
            before,
            await GetLessonAsync(lessonId));
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

        var userAccountId =
            await ExecuteDbAsync(
                dbContext =>
                    dbContext.UserAccounts
                        .Where(account =>
                            account.Email.Value == email)
                        .Select(account =>
                            account.Id.Value)
                        .SingleAsync());

        return new TestTutor(
            email,
            accessToken,
            userAccountId);
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
            await GetVerificationTokenAsync(email));

        var accessToken =
            (await LoginAsync(
                email,
                Password))
            .Login.AccessToken;

        var student =
            await ExecuteDbAsync(
                dbContext =>
                    dbContext.Students
                        .Where(item =>
                            item.Account != null &&
                            item.Account.Email.Value == email)
                        .Select(item => new
                        {
                            StudentId =
                                item.Id.Value,

                            UserAccountId =
                                item.UserAccountId!.Value.Value
                        })
                        .SingleAsync());

        return new TestStudent(
            accessToken,
            student.StudentId,
            student.UserAccountId);
    }

    private Task<Guid> CreateManagedStudentAsync(
        string displayName)
    {
        return ExecuteDbAsync(
            async dbContext =>
            {
                var student =
                    new Student(
                        new StudentDisplayName(
                            displayName),
                        DateTimeOffset.UtcNow);

                dbContext.Students.Add(
                    student);

                await dbContext.SaveChangesAsync();

                return student.Id.Value;
            });
    }

    private async Task<Guid> CreateAgreementAsync(
        string tutorEmail,
        Guid studentId)
    {
        return await ExecuteDbAsync(
            async dbContext =>
            {
                var tutorId =
                    await dbContext.Tutors
                        .Where(tutor =>
                            tutor.Account.Email.Value
                            == tutorEmail)
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
        Guid agreementId)
    {
        return await ExecuteDbAsync(
            async dbContext =>
            {
                var lesson =
                    new Lesson(
                        new TutoringAgreementId(
                            agreementId),
                        new TimeSlot(
                            StartsAtUtc,
                            StartsAtUtc.AddHours(1)),
                        DateTimeOffset.UtcNow);

                dbContext.Lessons.Add(
                    lesson);

                await dbContext.SaveChangesAsync();

                return lesson.Id.Value;
            });
    }

    private Task SetLessonStatusAsync(
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
                                lesson =>
                                    lesson.Status,
                                status)));
    }

    private Task<LessonSnapshot> GetLessonAsync(
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
                        new LessonSnapshot(
                            lesson.Id.Value,
                            lesson.Status,

                            lesson
                                .CancellationReason
                                == null
                                ? null
                                : lesson
                                    .CancellationReason
                                    .Text,

                            lesson
                                .LessonCancellationParty,

                            lesson
                                .CancelledAtUtc,

                            lesson
                                .CancelledByUserAccountId
                                == null
                                ? null
                                : lesson
                                    .CancelledByUserAccountId
                                    .Value.Value))
                    .SingleAsync());
    }

    private Task<HttpResponseMessage> CancelLessonAsync(
        string accessToken,
        Guid lessonId,
        LessonCancellationParty? cancellationParty,
        string? reason)
    {
        return Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                $"{Endpoint}/{lessonId}/cancel",
                accessToken,
                new
                {
                    CancellationParty =
                        cancellationParty,

                    Reason =
                        reason
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
        string AccessToken,
        Guid UserAccountId);

    private sealed record TestStudent(
        string AccessToken,
        Guid StudentId,
        Guid UserAccountId);

    private sealed record LessonSnapshot(
        Guid Id,
        LessonStatus Status,
        string? CancellationReason,
        LessonCancellationParty? LessonCancellationParty,
        DateTimeOffset? CancelledAtUtc,
        Guid? CancelledByUserAccountId);
}