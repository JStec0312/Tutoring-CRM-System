using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Lessons.GetMyLessons;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Lessons.GetMyLessons;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetMyLessonsTests(IntegrationTestFixture fixture) : LessonIntegrationTestBase(fixture)
{
    protected override DateTimeOffset AgreementCreatedAtUtc { get; } = DateTimeOffset.UtcNow.AddDays(-30);

    [Fact]
    public async Task Pages_AreBoundedStableAndScopedToTheStudentOrTutor()
    {
        var tutor = await CreateTutorAsync("page-tutor@test.pl", "page-tutor");
        var student = await CreateStudentAsync("page-student@test.pl", "page-student");
        var otherTutor = await CreateTutorAsync("other-tutor@test.pl", "other-tutor");
        var outsider = await CreateStudentAsync("other-student@test.pl", "other-student");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var foreignAgreement = await CreateAgreementAsync(otherTutor.Email, outsider.StudentId);
        var start = DateTimeOffset.UtcNow.AddDays(1);
        for (var index = 0; index < 5; index++)
        {
            await CreateLessonAsync(agreement, start.AddDays(index), start.AddDays(index).AddHours(1));
        }
        await CreateLessonAsync(foreignAgreement, start, start.AddHours(1));
        foreach (var (token, tutorOnly) in new[] { (student.AccessToken, false), (tutor.AccessToken, true) })
        {
            var first = await ReadAsync($"?page=1&pageSize=2&tutorOnly={tutorOnly}", token);
            var second = await ReadAsync($"?page=2&pageSize=2&tutorOnly={tutorOnly}", token);
            Assert.Equal(5, first.TotalCount);
            Assert.Equal(2, first.Items.Count);
            Assert.Equal(2, second.Page);
            Assert.Empty(first.Items.Select(item => item.LessonId).Intersect(second.Items.Select(item => item.LessonId)));
            Assert.All(first.Items.Concat(second.Items), item => Assert.Equal(agreement, item.TutoringAgreementId));
            var clamped = await ReadAsync($"?page=2147483647&pageSize=2&tutorOnly={tutorOnly}", token);
            Assert.Equal(3, clamped.Page);
            Assert.Single(clamped.Items);
        }
    }

    [Fact]
    public async Task Views_IncludeOnlyUpcomingScheduledOrPastAndCancelledLessons()
    {
        var tutor = await CreateTutorAsync("view-tutor@test.pl", "view-tutor");
        var student = await CreateStudentAsync("view-student@test.pl", "view-student");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var now = DateTimeOffset.UtcNow;
        var tomorrow = await CreateLessonAsync(agreement, now.AddDays(1), now.AddDays(1).AddHours(1));
        var later = await CreateLessonAsync(agreement, now.AddDays(2), now.AddDays(2).AddHours(1));
        var past = await CreateLessonAsync(agreement, now.AddDays(-2), now.AddDays(-2).AddHours(1));
        var cancelled = await CreateLessonAsync(agreement, now.AddDays(3), now.AddDays(3).AddHours(1));
        await ExecuteDbAsync(async db =>
        {
            var lesson = await db.Lessons.Include(item => item.Agreement).ThenInclude(item => item.Student)
                .SingleAsync(item => item.Id == new LessonId(cancelled));
            lesson.Cancel(LessonCancellationParty.Student, new CancellationReason("School trip"),
                lesson.Agreement.Student.UserAccountId!.Value, now);
            await db.SaveChangesAsync();
        });
        var upcoming = await ReadAsync("?view=upcoming", student.AccessToken);
        Assert.Equal(new[] { tomorrow, later }, upcoming.Items.Select(item => item.LessonId));
        var history = await ReadAsync("?view=past", student.AccessToken);
        Assert.Equal(2, history.TotalCount);
        Assert.Contains(history.Items, item => item.LessonId == past);
        Assert.Equal("School trip", Assert.Single(history.Items, item => item.LessonId == cancelled).CancellationReason);
    }

    [Fact]
    public async Task Page_ReturnsActualChargesTopicsAndNoPrivateTutorNotes()
    {
        var tutor = await CreateTutorAsync("payment-tutor@test.pl", "payment-tutor");
        var student = await CreateStudentAsync("payment-student@test.pl", "payment-student");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var now = DateTimeOffset.UtcNow;
        var unpaidId = await CreateLessonAsync(agreement, now.AddDays(-2), now.AddDays(-2).AddHours(1));
        var paidId = await CreateLessonAsync(agreement, now.AddDays(-1), now.AddDays(-1).AddHours(1));
        var scheduledId = await CreateLessonAsync(agreement, now.AddDays(1), now.AddDays(1).AddHours(1));
        await ExecuteDbAsync(async db =>
        {
            var unpaid = await db.Lessons.SingleAsync(item => item.Id == new LessonId(unpaidId));
            var paid = await db.Lessons.SingleAsync(item => item.Id == new LessonId(paidId));
            unpaid.Complete(now);
            paid.Complete(now);
            unpaid.SetPlannedTopic("Quadratic equations");
            var entity = await db.TutoringAgreements.SingleAsync(item => item.Id == new TutoringAgreementId(agreement));
            entity.UpdateTutoringAgreementDetails(null, "Private tutor observation");
            var account = new BillingAccount(entity.Id, now);
            account.AddLessonCharge(unpaid.Id, new Money(80, new Currency("PLN")), now);
            var charge = account.AddLessonCharge(paid.Id, new Money(30, new Currency("EUR")), now);
            account.MarkChargeAsPaid(charge.Id, now, null);
            db.BillingAccounts.Add(account);
            await db.SaveChangesAsync();
        });
        var page = await ReadAsync("", student.AccessToken);
        var unpaidLesson = Assert.Single(page.Items, item => item.LessonId == unpaidId);
        Assert.Equal("Quadratic equations", unpaidLesson.PlannedTopic);
        var unpaidCharge = Assert.Single(unpaidLesson.Charges);
        Assert.Equal(80, unpaidCharge.Amount);
        Assert.Null(unpaidCharge.PaidAtUtc);
        Assert.NotNull(Assert.Single(Assert.Single(page.Items, item => item.LessonId == paidId).Charges).PaidAtUtc);
        Assert.Empty(Assert.Single(page.Items, item => item.LessonId == scheduledId).Charges);
        using var raw = await GetAsync("/api/lessons", student.AccessToken);
        Assert.DoesNotContain("Private tutor observation", await raw.Content.ReadAsStringAsync());
        using var payment = CreateAuthorizedRequest(HttpMethod.Post, $"/api/tutors/me/lesson-charges/{unpaidCharge.ChargeId}/mark-paid", student.AccessToken);
        using var denied = await Client.SendAsync(payment);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Null(await ExecuteDbAsync(db => db.LessonCharges.Where(item => item.Id == new LessonChargeId(unpaidCharge.ChargeId)).Select(item => item.PaymentId).SingleAsync()));
    }

    [Fact]
    public async Task AuthenticationAndRequestedWorkspace_AreEnforced()
    {
        using var anonymous = await Client.GetAsync("/api/lessons");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        var tutor = await CreateTutorAsync("auth-tutor@test.pl", "auth-tutor");
        var student = await CreateStudentAsync("auth-student@test.pl", "auth-student");
        using var studentAsTutor = await GetAsync("/api/lessons?tutorOnly=true", student.AccessToken);
        using var tutorAsStudent = await GetAsync("/api/lessons", tutor.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, studentAsTutor.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, tutorAsStudent.StatusCode);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?page=-1")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=51")]
    [InlineData("?view=unknown")]
    public async Task InvalidPagingParameters_ReturnBadRequest(string query)
    {
        var student = await CreateStudentAsync("invalid-page@test.pl", "invalid-page");
        using var response = await GetAsync("/api/lessons" + query, student.AccessToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AgreementFilter_CannotReadAnotherStudentsLessonsAndEmptyPagesStayAtOne()
    {
        var tutor = await CreateTutorAsync("filter-tutor@test.pl", "filter-tutor");
        var owner = await CreateStudentAsync("filter-owner@test.pl", "filter-owner");
        var outsider = await CreateStudentAsync("filter-outsider@test.pl", "filter-outsider");
        var agreement = await CreateAgreementAsync(tutor.Email, owner.StudentId);
        var start = DateTimeOffset.UtcNow.AddDays(1);
        await CreateLessonAsync(agreement, start, start.AddHours(1));
        var page = await ReadAsync($"?agreementId={agreement}&page=50", outsider.AccessToken);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(1, page.Page);
        Assert.Empty(page.Items);
    }

    private async Task<LessonPageResponse> ReadAsync(string query, string token)
    {
        using var response = await GetAsync("/api/lessons" + query, token);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<LessonPageResponse>())!;
    }

    private async Task<HttpResponseMessage> GetAsync(string url, string token)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, url, token);
        return await Client.SendAsync(request);
    }
}
