using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Students.GetStudentAgreements;
using Tutoring.Api.Features.Students.GetStudentCharges;
using Tutoring.Api.Features.Students.GetStudentLesson;
using Tutoring.Api.Features.Students.GetStudentLessons;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Students.StudentWorkspace;

[Collection(IntegrationTestCollection.Name)]
public sealed class StudentWorkspaceTests(IntegrationTestFixture fixture) : LessonIntegrationTestBase(fixture)
{
    protected override DateTimeOffset AgreementCreatedAtUtc { get; } = DateTimeOffset.UtcNow.AddDays(-30);

    [Fact]
    public async Task Agreements_ReturnOnlyTheStudentsAgreementsIncludingThoseWithoutLessons()
    {
        var tutor = await CreateTutorAsync("student-tutor@test.pl", "student-tutor");
        var student = await CreateStudentAsync("workspace-student@test.pl", "workspace-student");
        var outsider = await CreateStudentAsync("workspace-other@test.pl", "workspace-other");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        await CreateAgreementAsync(tutor.Email, outsider.StudentId);
        await ExecuteDbAsync(async db =>
        {
            var entity = await db.TutoringAgreements.SingleAsync(item => item.Id == new TutoringAgreementId(agreement));
            entity.UpdateTutoringAgreementDetails(null, "Tutor private notes");
            await db.SaveChangesAsync();
        });

        using var response = await GetAsync("/api/students/me/agreements", student.AccessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = Assert.Single((await response.Content.ReadFromJsonAsync<StudentAgreementResponse[]>())!);
        Assert.Equal(agreement, item.AgreementId);
        Assert.Equal("Tutor", item.TutorFirstName);
        Assert.Equal(100m, item.HourlyRate);
        Assert.Equal("PLN", item.Currency);
        Assert.DoesNotContain("Tutor private notes", await response.Content.ReadAsStringAsync());
        using var noAgreements = await GetAsync("/api/students/me/agreements", (await CreateStudentAsync("new-student@test.pl", "new-student")).AccessToken);
        Assert.Empty((await noAgreements.Content.ReadFromJsonAsync<StudentAgreementResponse[]>())!);
    }

    [Fact]
    public async Task DetailsAndHistory_ReturnCurrentTopicAndCancellationWithoutTutorNotes()
    {
        var tutor = await CreateTutorAsync("lesson-tutor@test.pl", "lesson-tutor");
        var student = await CreateStudentAsync("lesson-student@test.pl", "lesson-student");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var lessonId = await CreateLessonAsync(agreement, start, start.AddMinutes(90));
        await ExecuteDbAsync(async db =>
        {
            var lesson = await db.Lessons.Include(item => item.Agreement).ThenInclude(item => item.Student)
                .SingleAsync(item => item.Id == new LessonId(lessonId));
            lesson.SetPlannedTopic("Quadratic equations");
            lesson.Agreement.UpdateTutoringAgreementDetails(null, "Private observation");
            lesson.Cancel(LessonCancellationParty.Student, new CancellationReason("School trip"),
                lesson.Agreement.Student.UserAccountId!.Value, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        });
        using var details = await GetAsync($"/api/students/me/lessons/{lessonId}", student.AccessToken);
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        var result = (await details.Content.ReadFromJsonAsync<StudentLessonDetailsResponse>())!;
        Assert.Equal("Quadratic equations", result.PlannedTopic);
        Assert.Equal("School trip", result.CancellationReason);
        Assert.Equal("Cancelled", result.Status);
        Assert.Equal(150m, result.EstimatedAmount);
        Assert.DoesNotContain("Private observation", await details.Content.ReadAsStringAsync());

        using var history = await GetAsync($"/api/students/me/agreements/{agreement}/lessons", student.AccessToken);
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        Assert.Equal(lessonId, Assert.Single((await history.Content.ReadFromJsonAsync<StudentLessonResponse[]>())!).LessonId);
    }

    [Fact]
    public async Task Charges_ReturnActualPaidAndUnpaidAmountsOnlyForSelectedAgreement()
    {
        var tutor = await CreateTutorAsync("charges-tutor@test.pl", "charges-tutor");
        var student = await CreateStudentAsync("charges-student@test.pl", "charges-student");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var otherAgreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var start = DateTimeOffset.UtcNow.AddDays(-2);
        var paidLesson = await CreateLessonAsync(agreement, start, start.AddHours(1));
        var unpaidLesson = await CreateLessonAsync(agreement, start.AddDays(1), start.AddDays(1).AddHours(1));
        var otherLesson = await CreateLessonAsync(otherAgreement, start, start.AddHours(1));
        await ExecuteDbAsync(async db =>
        {
            var now = DateTimeOffset.UtcNow;
            var account = new BillingAccount(new TutoringAgreementId(agreement), now);
            var charge = account.AddLessonCharge(new LessonId(paidLesson), new Money(80, new Currency("PLN")), now);
            account.AddLessonCharge(new LessonId(unpaidLesson), new Money(25, new Currency("EUR")), now);
            account.MarkChargeAsPaid(charge.Id, now, null);
            var other = new BillingAccount(new TutoringAgreementId(otherAgreement), now);
            other.AddLessonCharge(new LessonId(otherLesson), new Money(999, new Currency("PLN")), now);
            db.BillingAccounts.AddRange(account, other);
            await db.SaveChangesAsync();
        });
        using var response = await GetAsync($"/api/students/me/agreements/{agreement}/charges", student.AccessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var charges = (await response.Content.ReadFromJsonAsync<StudentChargeResponse[]>())!;
        Assert.Equal(2, charges.Length);
        Assert.NotNull(Assert.Single(charges, charge => charge.LessonId == paidLesson).PaidAtUtc);
        var unpaid = Assert.Single(charges, charge => charge.LessonId == unpaidLesson);
        Assert.Null(unpaid.PaidAtUtc);
        Assert.Equal(25, unpaid.Amount);
        Assert.Equal("EUR", unpaid.Currency);
    }

    [Theory]
    [InlineData("agreements")]
    [InlineData("agreements/00000000-0000-0000-0000-000000000001/lessons")]
    [InlineData("agreements/00000000-0000-0000-0000-000000000001/charges")]
    [InlineData("lessons/00000000-0000-0000-0000-000000000001")]
    public async Task Reads_RequireAuthenticationAndStudentRole(string path)
    {
        var url = $"/api/students/me/{path}";
        using var anonymous = await Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        var tutor = await CreateTutorAsync("wrong-role@test.pl", "wrong-role");
        using var denied = await GetAsync(url, tutor.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task ForeignAndMissingResources_ReturnTheSameNotFoundCodes()
    {
        var tutor = await CreateTutorAsync("scope-tutor@test.pl", "scope-tutor");
        var owner = await CreateStudentAsync("scope-owner@test.pl", "scope-owner");
        var outsider = await CreateStudentAsync("scope-outsider@test.pl", "scope-outsider");
        var agreement = await CreateAgreementAsync(tutor.Email, owner.StudentId);
        var now = DateTimeOffset.UtcNow;
        var lesson = await CreateLessonAsync(agreement, now, now.AddHours(1));
        foreach (var suffix in new[] { "lessons", "charges" })
        {
            foreach (var id in new[] { agreement, Guid.NewGuid() })
            {
                using var response = await GetAsync($"/api/students/me/agreements/{id}/{suffix}", outsider.AccessToken);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                Assert.Contains("Students.AgreementNotFound", await response.Content.ReadAsStringAsync());
            }
        }
        foreach (var id in new[] { lesson, Guid.NewGuid() })
        {
            using var response = await GetAsync($"/api/students/me/lessons/{id}", outsider.AccessToken);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Contains("Lessons.NotFoundForUser", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task OwnedAgreementWithoutLessonsOrCharges_ReturnsEmptyLists()
    {
        var tutor = await CreateTutorAsync("empty-tutor@test.pl", "empty-tutor");
        var student = await CreateStudentAsync("empty-student@test.pl", "empty-student");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        foreach (var suffix in new[] { "lessons", "charges" })
        {
            using var response = await GetAsync($"/api/students/me/agreements/{agreement}/{suffix}", student.AccessToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("[]", await response.Content.ReadAsStringAsync());
        }
    }

    private async Task<HttpResponseMessage> GetAsync(string url, string token)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, url, token);
        return await Client.SendAsync(request);
    }
}
