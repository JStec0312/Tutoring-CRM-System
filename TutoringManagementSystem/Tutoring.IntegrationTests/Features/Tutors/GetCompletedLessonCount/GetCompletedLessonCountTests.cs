using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Tutoring.Api.Features.Tutors.GetCompletedLessonCount;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Lessons;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.GetCompletedLessonCount;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetCompletedLessonCountTests(IntegrationTestFixture fixture) : LessonIntegrationTestBase(fixture)
{
    protected override DateTimeOffset AgreementCreatedAtUtc => DateTimeOffset.UtcNow.AddDays(-2);

    [Fact]
    public async Task CountsOnlyCompletedLessonsAcrossTheTutorsAgreements()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var otherTutor = await CreateTutorAsync("other@test.pl", "other-tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");
        var otherStudent = await CreateStudentAsync("other-student@test.pl", "other-student");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var secondAgreement = await CreateAgreementAsync(tutor.Email, otherStudent.StudentId);
        var foreignAgreement = await CreateAgreementAsync(otherTutor.Email, student.StudentId);
        var now = DateTimeOffset.UtcNow;
        var start = now.AddHours(-3);
        var end = now.AddHours(-2);
        var completed = new LessonId(await CreateLessonAsync(agreement, start, end));
        var secondCompleted = new LessonId(await CreateLessonAsync(secondAgreement, start, end));
        var foreignCompleted = new LessonId(await CreateLessonAsync(foreignAgreement, start, end));
        var missed = new LessonId(await CreateLessonAsync(agreement, start, end));
        var cancelled = new LessonId(await CreateLessonAsync(agreement, start, end));
        await CreateLessonAsync(agreement, start, end);
        var accountId = new UserAccountId(Guid.Parse(new JsonWebToken(tutor.AccessToken).Subject));

        await ExecuteDbAsync(async db =>
        {
            foreach (var lessonId in new[] { completed, secondCompleted, foreignCompleted })
                (await db.Lessons.SingleAsync(lesson => lesson.Id == lessonId)).Complete(now);
            (await db.Lessons.SingleAsync(lesson => lesson.Id == missed)).MarkAsMissed(now);
            (await db.Lessons.SingleAsync(lesson => lesson.Id == cancelled))
                .Cancel(LessonCancellationParty.Tutor, null, accountId, now);
            await db.SaveChangesAsync();
        });

        Assert.Equal(2, await ReadCountAsync(tutor.AccessToken));
        Assert.Equal(1, await ReadCountAsync(otherTutor.AccessToken));
    }

    [Fact]
    public async Task TutorWithoutLessons_ReturnsZero()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        Assert.Equal(0, await ReadCountAsync(tutor.AccessToken));
    }

    [Fact]
    public async Task RequiresTutorRole()
    {
        var student = await CreateStudentAsync("student@test.pl", "student");
        using var anonymous = await GetAsync(null);
        using var forbidden = await GetAsync(student.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    private async Task<int> ReadCountAsync(string token)
    {
        using var response = await GetAsync(token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CompletedLessonCountResponse>();
        return Assert.IsType<CompletedLessonCountResponse>(body).CompletedLessonCount;
    }

    private async Task<HttpResponseMessage> GetAsync(string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/tutors/me/lessons/completed-count");
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await Client.SendAsync(request);
    }
}
