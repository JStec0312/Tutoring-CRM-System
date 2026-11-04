using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.GetStudentStatistics;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.GetStudentStatistics;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetStudentStatisticsTests(IntegrationTestFixture fixture) : LessonIntegrationTestBase(fixture)
{
    private static readonly DateTimeOffset Day = new(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);
    protected override DateTimeOffset AgreementCreatedAtUtc => Day.AddMonths(-1);

    [Fact]
    public async Task AggregatesOwnedLessonsAndChargesByInclusiveLessonDate()
    {
        var tutor = await CreateTutorAsync("owner@test.pl", "owner");
        var otherTutor = await CreateTutorAsync("other@test.pl", "other");
        var student = await CreateStudentAsync("student@test.pl", "student");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var secondAgreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var foreignAgreement = await CreateAgreementAsync(otherTutor.Email, student.StudentId);

        var completed = await CreateLessonAsync(agreement, Day, Day.AddMinutes(45));
        var missed = await CreateLessonAsync(secondAgreement, Day.AddHours(1), Day.AddHours(2));
        var scheduled = await CreateLessonAsync(agreement, Day.AddDays(1).AddTicks(-1), Day.AddDays(1).AddHours(1));
        var excluded = await CreateLessonAsync(agreement, Day.AddDays(1), Day.AddDays(1).AddHours(2));
        await CreateLessonAsync(agreement, Day.AddDays(-1), Day.AddDays(-1).AddHours(1));
        await CreateLessonAsync(foreignAgreement, Day, Day.AddHours(1));

        await ExecuteDbAsync(async db =>
        {
            var lesson = await db.Lessons.SingleAsync(lesson => lesson.Id == new LessonId(completed));
            lesson.Complete(Day.AddDays(2));
            lesson = await db.Lessons.SingleAsync(lesson => lesson.Id == new LessonId(missed));
            lesson.MarkAsMissed(Day.AddDays(2));

            var account = new BillingAccount(new TutoringAgreementId(agreement), AgreementCreatedAtUtc);
            var paid = account.AddLessonCharge(new LessonId(completed), new Money(75m, new Currency("PLN")), Day);
            var outsideRange = account.AddLessonCharge(new LessonId(excluded), new Money(100m, new Currency("PLN")), Day);
            account.MarkChargesAsPaid([paid.Id, outsideRange.Id], Day.AddDays(2), null);
            account.AddLessonCharge(new LessonId(scheduled), new Money(10m, new Currency("EUR")), Day);
            db.BillingAccounts.Add(account);
            await db.SaveChangesAsync();
        });

        var stats = await GetStatisticsAsync(tutor, student, "?from=2026-01-15&to=2026-01-15");
        Assert.Equal(new StudentLessonStatistics(1, 1, 0, 1, 45m), stats.Lessons);
        Assert.Equal(new StudentAttendanceStatistics(50m, 0, 0), stats.Attendance);
        Assert.Equal(
            [new StudentPaymentStatistics("EUR", 0m, 10m, 1), new StudentPaymentStatistics("PLN", 75m, 0m, 0)],
            stats.Payments);

        Assert.Equal(3, (await GetStatisticsAsync(tutor, student)).Lessons.ScheduledCount);
        Assert.Equal(2, (await GetStatisticsAsync(tutor, student, "?from=2026-01-15")).Lessons.ScheduledCount);
        Assert.Equal(2, (await GetStatisticsAsync(tutor, student, "?to=2026-01-15")).Lessons.ScheduledCount);
    }

    [Fact]
    public async Task EmptyRangeReturnsZeroCountsAndUndefinedAttendance()
    {
        var tutor = await CreateTutorAsync("owner@test.pl", "owner");
        var student = await CreateStudentAsync("student@test.pl", "student");
        await CreateAgreementAsync(tutor.Email, student.StudentId);

        var stats = await GetStatisticsAsync(tutor, student);

        Assert.Equal(new StudentLessonStatistics(0, 0, 0, 0, 0m), stats.Lessons);
        Assert.Equal(new StudentAttendanceStatistics(null, 0, 0), stats.Attendance);
        Assert.Empty(stats.Payments);
    }

    [Fact]
    public async Task RequiresTutorRoleAndAnOwnedAgreement()
    {
        var tutor = await CreateTutorAsync("owner@test.pl", "owner");
        var otherTutor = await CreateTutorAsync("other@test.pl", "other");
        var student = await CreateStudentAsync("student@test.pl", "student");
        await CreateAgreementAsync(otherTutor.Email, student.StudentId);

        using var anonymous = await Client.GetAsync(Endpoint(student.StudentId));
        using var studentRequest = CreateAuthorizedRequest(HttpMethod.Get, Endpoint(student.StudentId), student.AccessToken);
        using var forbidden = await Client.SendAsync(studentRequest);
        using var tutorRequest = CreateAuthorizedRequest(HttpMethod.Get, Endpoint(student.StudentId), tutor.AccessToken);
        using var notFound = await Client.SendAsync(tutorRequest);
        using var unknownRequest = CreateAuthorizedRequest(HttpMethod.Get, Endpoint(Guid.NewGuid()), tutor.AccessToken);
        using var unknown = await Client.SendAsync(unknownRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Contains("TutoringAgreements.NotFound", await notFound.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("?from=invalid")]
    [InlineData("?to=invalid")]
    [InlineData("?from=2026-01-16&to=2026-01-15")]
    public async Task InvalidRangeReturnsBadRequest(string query)
    {
        var tutor = await CreateTutorAsync("owner@test.pl", "owner");
        var student = await CreateStudentAsync("student@test.pl", "student");
        await CreateAgreementAsync(tutor.Email, student.StudentId);

        using var request = CreateAuthorizedRequest(HttpMethod.Get, Endpoint(student.StudentId) + query, tutor.AccessToken);
        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        if (query.Contains('&'))
        {
            Assert.Contains("Request.Invalid", await response.Content.ReadAsStringAsync());
        }
    }

    private static string Endpoint(Guid studentId) => $"/api/tutors/me/students/{studentId}/statistics";

    private async Task<StudentStatisticsResponse> GetStatisticsAsync(TestTutor tutor, TestStudent student, string query = "")
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, Endpoint(student.StudentId) + query, tutor.AccessToken);
        using var response = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<StudentStatisticsResponse>(await response.Content.ReadFromJsonAsync<StudentStatisticsResponse>());
    }
}
