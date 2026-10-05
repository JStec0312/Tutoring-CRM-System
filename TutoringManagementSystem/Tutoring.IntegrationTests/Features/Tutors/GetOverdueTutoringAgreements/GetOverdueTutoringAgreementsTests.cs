using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.GetOverdueTutoringAgreements;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Domain.Tutors;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.GetOverdueTutoringAgreements;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetOverdueTutoringAgreementsTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/tutoring-agreements/overdue";

    [Fact]
    public async Task GetOverdueAgreements_ShouldAggregateUnpaidChargesPerAgreementAndFilterResults()
    {
        var tutor = await CreateTutorAsync("overdue-owner@test.pl", "overdue-owner");
        var otherTutor = await CreateTutorAsync("overdue-other@test.pl", "overdue-other");
        var sharedStudentId = await CreateManagedStudentAsync("Shared overdue student");

        var suspendedAgreement = await CreateAgreementWithChargesAsync(
            tutor.TutorId,
            sharedStudentId,
            "Suspended agreement",
            [10m, 20m, 30m, 40m, 60m],
            paidChargeIndexes: [0]);
        await SetChargeStatusAsync(suspendedAgreement.ChargeIds[1], ChargeStatus.Cancelled);
        await SetChargeStatusAsync(suspendedAgreement.ChargeIds[2], ChargeStatus.Corrected);
        await SetAgreementStatusAsync(suspendedAgreement.TutoringAgreementId, AgreementStatus.Suspended);

        var endedAgreement = await CreateAgreementWithChargesAsync(
            tutor.TutorId,
            sharedStudentId,
            "Ended agreement",
            [150m]);
        await SetAgreementStatusAsync(endedAgreement.TutoringAgreementId, AgreementStatus.Ended);

        await CreateAgreementWithChargesAsync(
            tutor.TutorId,
            sharedStudentId,
            "Fully paid agreement",
            [75m],
            paidChargeIndexes: [0]);

        var cancelledAgreement = await CreateAgreementWithChargesAsync(
            tutor.TutorId,
            sharedStudentId,
            "Cancelled-only agreement",
            [90m]);
        await SetChargeStatusAsync(cancelledAgreement.ChargeIds[0], ChargeStatus.Cancelled);

        var foreignStudentId = await CreateManagedStudentAsync("Other tutor student");
        await CreateAgreementWithChargesAsync(
            otherTutor.TutorId,
            foreignStudentId,
            "Foreign agreement",
            [500m]);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            Endpoint,
            tutor.AccessToken);
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var agreements = await response.Content
            .ReadFromJsonAsync<List<OverdueTutoringAgreementResponse>>();

        Assert.NotNull(agreements);
        Assert.Equal(2, agreements.Count);
        Assert.Equal(
            [endedAgreement.TutoringAgreementId, suspendedAgreement.TutoringAgreementId],
            agreements.Select(agreement => agreement.TutoringAgreementId));

        var endedResult = agreements[0];
        Assert.Equal(sharedStudentId, endedResult.StudentId);
        Assert.Equal("Shared overdue student", endedResult.StudentDisplayName);
        Assert.Equal("Mathematics", endedResult.Subject);
        Assert.Equal("Ended agreement", endedResult.AgreementTitle);
        Assert.Equal(150m, endedResult.OutstandingAmount);
        Assert.Equal(1, endedResult.UnpaidLessonCount);

        var suspendedResult = agreements[1];
        Assert.Equal(sharedStudentId, suspendedResult.StudentId);
        Assert.Equal("Suspended agreement", suspendedResult.AgreementTitle);
        Assert.Equal(100m, suspendedResult.OutstandingAmount);
        Assert.Equal(2, suspendedResult.UnpaidLessonCount);
    }

    [Fact]
    public async Task GetOverdueAgreements_WhenNoUnpaidActiveCharges_ShouldReturnEmptyCollection()
    {
        var tutor = await CreateTutorAsync("overdue-empty@test.pl", "overdue-empty");
        var studentId = await CreateManagedStudentAsync("Settled student");
        var paidAgreement = await CreateAgreementWithChargesAsync(
            tutor.TutorId,
            studentId,
            "Paid agreement",
            [25m],
            paidChargeIndexes: [0]);
        var cancelledAgreement = await CreateAgreementWithChargesAsync(
            tutor.TutorId,
            studentId,
            "Cancelled agreement",
            [50m]);
        await SetChargeStatusAsync(cancelledAgreement.ChargeIds[0], ChargeStatus.Cancelled);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            Endpoint,
            tutor.AccessToken);
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var agreements = await response.Content
            .ReadFromJsonAsync<List<OverdueTutoringAgreementResponse>>();

        Assert.NotNull(agreements);
        Assert.Empty(agreements);
    }

    [Fact]
    public async Task GetOverdueAgreements_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await Client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOverdueAgreements_AsStudent_ShouldReturnForbidden()
    {
        const string email = "overdue-student@test.pl";
        await RegisterAndConfirmStudentAsync(email, Password);
        var student = await LoginAsync(email, Password);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            Endpoint,
            student.Login.AccessToken);
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<TestTutor> CreateTutorAsync(string email, string userName)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/tutor",
            new
            {
                Email = email,
                UserName = userName,
                Password,
                FirstName = "Test",
                LastName = "Tutor",
                PhoneNumber = (string?)null
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await ConfirmEmailAsync(await GetVerificationTokenAsync(email));
        var session = await LoginAsync(email, Password);
        var tutorId = await ExecuteDbAsync(db => db.Tutors
            .AsNoTracking()
            .Where(tutor => tutor.Account.Email.Value == email)
            .Select(tutor => tutor.Id.Value)
            .SingleAsync());

        return new TestTutor(tutorId, session.Login.AccessToken);
    }

    private Task<Guid> CreateManagedStudentAsync(string displayName)
    {
        return ExecuteDbAsync(async db =>
        {
            var student = new Student(
                new StudentDisplayName(displayName),
                DateTimeOffset.UtcNow);
            db.Students.Add(student);
            await db.SaveChangesAsync();
            return student.Id.Value;
        });
    }

    private Task<AgreementCharges> CreateAgreementWithChargesAsync(
        Guid tutorId,
        Guid studentId,
        string agreementTitle,
        IReadOnlyList<decimal> amounts,
        IReadOnlyCollection<int>? paidChargeIndexes = null)
    {
        return ExecuteDbAsync(async db =>
        {
            var now = DateTimeOffset.UtcNow;
            var agreement = new TutoringAgreement(
                new TutorId(tutorId),
                new StudentId(studentId),
                new Subject("Mathematics"),
                new HourlyRate(new Money(100m, new Currency("PLN"))),
                new AgreementTitle(agreementTitle),
                now);
            var account = new BillingAccount(agreement.Id, now);
            var lessons = new List<Lesson>();
            var charges = new List<LessonCharge>();

            for (var index = 0; index < amounts.Count; index++)
            {
                var startsAtUtc = now.AddHours(index * 2);
                var lesson = new Lesson(
                    agreement.Id,
                    new TimeSlot(startsAtUtc, startsAtUtc.AddHours(1)),
                    now);
                var charge = account.AddLessonCharge(
                    lesson.Id,
                    new Money(amounts[index], new Currency("PLN")),
                    now);

                lessons.Add(lesson);
                charges.Add(charge);
            }

            if (paidChargeIndexes is { Count: > 0 })
            {
                account.MarkChargesAsPaid(
                    paidChargeIndexes.Select(index => charges[index].Id).ToArray(),
                    now,
                    null);
            }

            db.TutoringAgreements.Add(agreement);
            db.BillingAccounts.Add(account);
            db.Lessons.AddRange(lessons);
            await db.SaveChangesAsync();

            return new AgreementCharges(
                agreement.Id.Value,
                charges.Select(charge => charge.Id.Value).ToArray());
        });
    }

    private Task SetChargeStatusAsync(Guid chargeId, ChargeStatus status)
    {
        return ExecuteDbAsync(async db =>
        {
            var charge = await db.LessonCharges.SingleAsync(
                item => item.Id == new LessonChargeId(chargeId));
            db.Entry(charge).Property(item => item.Status).CurrentValue = status;
            await db.SaveChangesAsync();
        });
    }

    private Task SetAgreementStatusAsync(Guid agreementId, AgreementStatus status)
    {
        return ExecuteDbAsync(async db =>
        {
            var agreement = await db.TutoringAgreements.SingleAsync(
                item => item.Id == new TutoringAgreementId(agreementId));
            db.Entry(agreement).Property(item => item.Status).CurrentValue = status;
            await db.SaveChangesAsync();
        });
    }

    private sealed record TestTutor(Guid TutorId, string AccessToken);

    private sealed record AgreementCharges(
        Guid TutoringAgreementId,
        IReadOnlyList<Guid> ChargeIds);
}