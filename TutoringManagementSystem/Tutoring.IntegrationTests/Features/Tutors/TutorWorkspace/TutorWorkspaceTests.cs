using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tutoring.Api.Features.Calendar.GetCalendarLessons;
using Tutoring.Api.Features.LearningMaterials.GetLearningMaterials;
using Tutoring.Api.Features.Tutors.GetTutorLesson;
using Tutoring.Api.Features.Tutors.ScheduleLesson;
using Tutoring.Api.Features.Users.GetProfile;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.TutorWorkspace;

[Collection(IntegrationTestCollection.Name)]
public sealed class TutorWorkspaceTests(IntegrationTestFixture fixture) : LessonIntegrationTestBase(fixture)
{
    protected override DateTimeOffset AgreementCreatedAtUtc { get; } = DateTimeOffset.UtcNow.AddDays(-7);

    [Fact]
    public async Task SchedulingAndEditing_PersistsTopicAndPreservesItWhenOmitted()
    {
        var tutor = await CreateTutorAsync("topic-tutor@test.pl", "topic-tutor");
        var student = await CreateStudentAsync("topic-student@test.pl", "topic-student");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var start = DateTimeOffset.UtcNow.AddDays(2);
        const string endpoint = "/api/tutors/me/lessons";
        using var schedule = CreateAuthorizedRequest(HttpMethod.Post, endpoint, tutor.AccessToken,
            new { TutoringAgreementId = agreement, StartsAt = start, DurationMinutes = 60, Topic = " Fractions " });
        using var scheduled = await Client.SendAsync(schedule);
        Assert.Equal(HttpStatusCode.Created, scheduled.StatusCode);
        var id = (await scheduled.Content.ReadFromJsonAsync<ScheduleLessonResponse>())!.LessonId;
        using var details = await GetAsync($"{endpoint}/{id}", tutor.AccessToken);
        Assert.Equal("Fractions", (await details.Content.ReadFromJsonAsync<TutorLessonResponse>())!.PlannedTopic);

        using var reschedule = CreateAuthorizedRequest(HttpMethod.Put, $"{endpoint}/{id}/schedule", tutor.AccessToken,
            new { StartsAt = start.AddDays(1), DurationMinutes = 90 });
        using var rescheduled = await Client.SendAsync(reschedule);
        Assert.Equal(HttpStatusCode.NoContent, rescheduled.StatusCode);
        using var changedDetails = await GetAsync($"{endpoint}/{id}", tutor.AccessToken);
        var changed = (await changedDetails.Content.ReadFromJsonAsync<TutorLessonResponse>())!;
        Assert.Equal("Fractions", changed.PlannedTopic);
        Assert.Equal(start.AddDays(1), changed.Lesson.StartsAtUtc);
    }

    [Fact]
    public async Task MaterialsList_ReturnsMetadataToParticipantsOnly()
    {
        var tutor = await CreateTutorAsync("materials-owner@test.pl", "materials-owner");
        var student = await CreateStudentAsync("materials-student@test.pl", "materials-student");
        var outsider = await CreateTutorAsync("materials-other@test.pl", "materials-other");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var lesson = await CreateLessonAsync(agreement, start, start.AddHours(1));
        const string endpoint = "/api/learning-materials";
        using var upload = CreateAuthorizedRequest(HttpMethod.Post, endpoint, student.AccessToken);
        using var file = new ByteArrayContent("%PDF-1.7\nTest material"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        using var form = new MultipartFormDataContent
        {
            { new StringContent("Notes"), "Title" },
            { new StringContent(agreement.ToString()), "TutoringAgreementId" },
            { new StringContent(lesson.ToString()), "LessonId" },
            { file, "File", "notes.pdf" }
        };
        upload.Content = form;
        using var uploaded = await Client.SendAsync(upload);
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);

        var url = $"{endpoint}?tutoringAgreementId={agreement}";
        using var unauthorized = await Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        using var denied = await GetAsync(url, outsider.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        foreach (var token in new[] { tutor.AccessToken, student.AccessToken })
        {
            using var allowed = await GetAsync(url, token);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            var material = Assert.Single((await allowed.Content.ReadFromJsonAsync<LearningMaterialResponse[]>())!);
            Assert.Equal(lesson, material.LessonId);
            Assert.Equal("notes.pdf", material.FileName);
            Assert.False(material.UploadedByTutor);
            Assert.DoesNotContain("storageLocation", await allowed.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task CalendarAgreementFilter_RestrictsLessonsWithinTutorScope()
    {
        var tutor = await CreateTutorAsync("calendar-owner@test.pl", "calendar-owner");
        var outsider = await CreateTutorAsync("calendar-other@test.pl", "calendar-other");
        var student = await CreateStudentAsync("calendar-student@test.pl", "calendar-student");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var otherAgreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var start = DateTimeOffset.UtcNow.AddDays(2);
        var lesson = await CreateLessonAsync(agreement, start, start.AddHours(1));
        await CreateLessonAsync(otherAgreement, start.AddHours(1), start.AddHours(2));
        var url = $"/api/calendar?from={Uri.EscapeDataString(start.AddDays(-1).ToString("O"))}" +
            $"&to={Uri.EscapeDataString(start.AddDays(1).ToString("O"))}&tutorOnly=true&agreementId={agreement}";

        using var response = await GetAsync(url, tutor.AccessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(lesson, Assert.Single((await response.Content.ReadFromJsonAsync<CalendarLessonResponse[]>())!).LessonId);
        using var other = await GetAsync(url, outsider.AccessToken);
        Assert.Empty((await other.Content.ReadFromJsonAsync<CalendarLessonResponse[]>())!);
    }

    [Fact]
    public async Task ExistingStudentAgreement_RequiresExistingTutorRelationship()
    {
        var tutor = await CreateTutorAsync("agreements-owner@test.pl", "agreements-owner");
        var outsider = await CreateTutorAsync("agreements-other@test.pl", "agreements-other");
        var student = await CreateStudentAsync("agreements-student@test.pl", "agreements-student");
        await CreateAgreementAsync(tutor.Email, student.StudentId);
        var url = $"/api/tutors/me/students/{student.StudentId}/agreements";
        var body = new { Title = "Physics lessons", Subject = "Physics", HourlyRate = 120 };

        using var forbidden = CreateAuthorizedRequest(HttpMethod.Post, url, student.AccessToken, body);
        using var forbiddenResponse = await Client.SendAsync(forbidden);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        using var denied = CreateAuthorizedRequest(HttpMethod.Post, url, outsider.AccessToken, body);
        using var deniedResponse = await Client.SendAsync(denied);
        Assert.True(deniedResponse.StatusCode == HttpStatusCode.NotFound, await deniedResponse.Content.ReadAsStringAsync());
        using var allowed = CreateAuthorizedRequest(HttpMethod.Post, url, tutor.AccessToken, body);
        using var allowedResponse = await Client.SendAsync(allowed);
        Assert.Equal(HttpStatusCode.Created, allowedResponse.StatusCode);
        using var students = await GetAsync("/api/tutors/me/students", tutor.AccessToken);
        var record = Assert.Single((await students.Content.ReadFromJsonAsync<StudentSummary[]>())!);
        Assert.Equal(student.StudentId, record.StudentId);
        Assert.Equal(2, record.Agreements.Length);
    }

    [Fact]
    public async Task ProfileAndPassword_UseTheAuthenticatedAccount()
    {
        var tutor = await CreateTutorAsync("profile-owner@test.pl", "profile-owner");
        const string profileUrl = "/api/users/me/profile";
        const string passwordUrl = "/api/users/me/password";
        using var anonymous = await Client.GetAsync(profileUrl);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using var profile = await GetAsync(profileUrl, tutor.AccessToken);
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        Assert.Equal("Tutor", (await profile.Content.ReadFromJsonAsync<ProfileResponse>())!.FirstName);

        using var invalid = CreateAuthorizedRequest(HttpMethod.Put, passwordUrl, tutor.AccessToken,
            new { CurrentPassword = "incorrect", NewPassword = "AnotherPassword123!" });
        using var invalidResponse = await Client.SendAsync(invalid);
        Assert.True(invalidResponse.StatusCode == HttpStatusCode.BadRequest, await invalidResponse.Content.ReadAsStringAsync());
        using var valid = CreateAuthorizedRequest(HttpMethod.Put, passwordUrl, tutor.AccessToken,
            new { CurrentPassword = "Password123!", NewPassword = "AnotherPassword123!" });
        using var validResponse = await Client.SendAsync(valid);
        Assert.Equal(HttpStatusCode.NoContent, validResponse.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace((await LoginAsync(tutor.Email, "AnotherPassword123!")).Login.AccessToken));
    }

    private async Task<HttpResponseMessage> GetAsync(string url, string token)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, url, token);
        return await Client.SendAsync(request);
    }

    private sealed record StudentSummary(Guid StudentId, AgreementSummary[] Agreements);
    private sealed record AgreementSummary(Guid TutoringAgreementId);
}
