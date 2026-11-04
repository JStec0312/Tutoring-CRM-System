using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Lessons;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.SaveLessonNote;

[Collection(IntegrationTestCollection.Name)]
public sealed class SaveLessonNoteTests(IntegrationTestFixture fixture) : LessonIntegrationTestBase(fixture)
{
    protected override DateTimeOffset AgreementCreatedAtUtc => DateTimeOffset.UtcNow.AddDays(-2);

    [Fact]
    public async Task Owner_CreatesAndReplacesOneNote()
    {
        var (tutor, _, lessonId) = await CreateContextAsync();
        using var created = await SaveAsync(lessonId, tutor.AccessToken, new(" Fractions ", " Addition ", " Improving "));
        Assert.Equal(HttpStatusCode.NoContent, created.StatusCode);
        var initial = await ExecuteDbAsync(db => db.LessonNotes.SingleAsync());
        Assert.Equal(new LessonId(lessonId), initial.LessonId);
        Assert.Equal(("Fractions", "Addition", "Improving"),
            (initial.Topic, initial.CoveredTopics, initial.StudentProgress));

        using var updated = await SaveAsync(lessonId, tutor.AccessToken, new("Decimals", "Division", "Independent"));
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        var current = await ExecuteDbAsync(db => db.LessonNotes.SingleAsync());
        Assert.Equal(initial.Id, current.Id);
        Assert.Equal(("Decimals", "Division", "Independent"),
            (current.Topic, current.CoveredTopics, current.StudentProgress));
    }

    [Fact]
    public async Task ConcurrentFirstWrites_LeaveOneCompleteNote()
    {
        var (tutor, _, lessonId) = await CreateContextAsync();
        var first = new NoteBody("Fractions", "Addition", "Improving");
        var second = new NoteBody("Decimals", "Division", "Independent");
        var responses = await Task.WhenAll(
            SaveAsync(lessonId, tutor.AccessToken, first), SaveAsync(lessonId, tutor.AccessToken, second));
        foreach (var response in responses)
        {
            using (response)
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        var stored = await ExecuteDbAsync(db => db.LessonNotes.SingleAsync());
        Assert.Contains(new NoteBody(stored.Topic, stored.CoveredTopics, stored.StudentProgress), new[] { first, second });
    }

    [Theory]
    [InlineData(null, 10, 10)]
    [InlineData(" ", 10, 10)]
    [InlineData("Topic", 0, 10)]
    [InlineData("Topic", 10, 2001)]
    [InlineData("Topic", 2001, 10)]
    public async Task InvalidFields_Return422WithoutOverwritingNote(string? topic, int coveredLength, int progressLength)
    {
        var (tutor, _, lessonId) = await CreateContextAsync();
        using var original = await SaveAsync(lessonId, tutor.AccessToken, new("Original", "Covered", "Progress"));
        Assert.Equal(HttpStatusCode.NoContent, original.StatusCode);

        using var response = await SaveAsync(lessonId, tutor.AccessToken,
            new(topic, new string('c', coveredLength), new string('p', progressLength)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        await AssertProblemCodeAsync(response, "Lessons.InvalidNote");
        var stored = await ExecuteDbAsync(db => db.LessonNotes.SingleAsync());
        Assert.Equal(("Original", "Covered", "Progress"), (stored.Topic, stored.CoveredTopics, stored.StudentProgress));
    }

    [Fact]
    public async Task UnauthorizedAndMissingRequests_DoNotCreateNotes()
    {
        var (tutor, student, lessonId) = await CreateContextAsync();
        var other = await CreateTutorAsync("other@test.pl", "other-tutor");
        var body = new NoteBody("Fractions", "Addition", "Improving");
        using var anonymous = await SaveAsync(lessonId, null, body);
        using var forbidden = await SaveAsync(lessonId, student.AccessToken, body);
        using var foreign = await SaveAsync(lessonId, other.AccessToken, body);
        using var missing = await SaveAsync(Guid.NewGuid(), tutor.AccessToken, body);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await AssertProblemCodeAsync(foreign, "Lessons.LessonNotFound");
        await AssertProblemCodeAsync(missing, "Lessons.LessonNotFound");
        Assert.False(await ExecuteDbAsync(db => db.LessonNotes.AnyAsync()));
    }

    private async Task<(TestTutor Tutor, TestStudent Student, Guid LessonId)> CreateContextAsync()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");
        var agreementId = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var startsAt = DateTimeOffset.UtcNow.AddHours(-3);
        return (tutor, student, await CreateLessonAsync(agreementId, startsAt, startsAt.AddHours(1)));
    }

    private async Task<HttpResponseMessage> SaveAsync(Guid lessonId, string? token, NoteBody body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/tutors/me/lessons/{lessonId}/note")
        {
            Content = JsonContent.Create(body)
        };
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await Client.SendAsync(request);
    }

    private sealed record NoteBody(string? Topic, string CoveredTopics, string StudentProgress);
}
