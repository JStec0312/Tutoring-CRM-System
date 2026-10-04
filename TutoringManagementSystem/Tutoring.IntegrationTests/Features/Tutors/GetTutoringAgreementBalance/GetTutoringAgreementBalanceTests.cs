using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.GetTutoringAgreementBalance;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Domain.Tutors;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.GetTutoringAgreementBalance;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetTutoringAgreementBalanceTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/tutoring-agreements";

    [Fact]
    public async Task GetBalance_ShouldSumActiveChargesByPaymentLink()
    {
        var tutor = await CreateTutorAsync("balance-owner@test.pl", "balance-owner");
        var agreementData = await CreateChargeSetAsync(
            tutor.TutorId,
            [45.50m, 30m, 12.50m, 7.25m],
            new DateTimeOffset(2020, 1, 1, 10, 0, 0, TimeSpan.Zero),
            paidChargeCount: 2);
        await CreateChargeSetAsync(
            tutor.TutorId,
            [250m],
            new DateTimeOffset(2019, 1, 1, 10, 0, 0, TimeSpan.Zero));

        await ExecuteDbAsync(async db =>
        {
            var cancelledCharge = await db.LessonCharges.SingleAsync(
                charge => charge.Id == new LessonChargeId(agreementData.ChargeIds[2]));
            db.Entry(cancelledCharge).Property(charge => charge.Status).CurrentValue =
                ChargeStatus.Cancelled;

            db.Payments.Add(new Payment(
                new BillingAccountId(agreementData.BillingAccountId),
                new Money(500m, new Currency("PLN")),
                DateTimeOffset.UtcNow,
                null));

            await db.SaveChangesAsync();
        });

        var response = await GetBalanceAsync(
            tutor.AccessToken,
            agreementData.TutoringAgreementId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var balance = await response.Content
            .ReadFromJsonAsync<GetTutoringAgreementBalanceResponse>();

        Assert.NotNull(balance);
        Assert.Equal(82.75m, balance.TotalCharged);
        Assert.Equal(75.50m, balance.TotalPaid);
        Assert.Equal(7.25m, balance.Balance);
        Assert.Equal(balance.TotalCharged - balance.TotalPaid, balance.Balance);
    }

    [Fact]
    public async Task GetBalance_WhenAgreementHasNoBillingAccount_ShouldReturnZeroTotals()
    {
        var tutor = await CreateTutorAsync("balance-no-account@test.pl", "balance-no-account");
        var agreementId = await CreateAgreementWithoutBillingAccountAsync(tutor.TutorId);

        var response = await GetBalanceAsync(tutor.AccessToken, agreementId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var balance = await response.Content
            .ReadFromJsonAsync<GetTutoringAgreementBalanceResponse>();

        Assert.NotNull(balance);
        Assert.Equal(0m, balance.TotalCharged);
        Assert.Equal(0m, balance.TotalPaid);
        Assert.Equal(0m, balance.Balance);
    }

    [Fact]
    public async Task GetBalance_WhenAgreementIsMissingOrOwnedByAnotherTutor_ShouldReturnNotFound()
    {
        var owner = await CreateTutorAsync("balance-other-owner@test.pl", "balance-other-owner");
        var otherTutor = await CreateTutorAsync("balance-other-tutor@test.pl", "balance-other-tutor");
        var agreementId = await CreateAgreementWithoutBillingAccountAsync(owner.TutorId);

        var foreignAgreementResponse = await GetBalanceAsync(
            otherTutor.AccessToken,
            agreementId);
        var missingAgreementResponse = await GetBalanceAsync(
            otherTutor.AccessToken,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, foreignAgreementResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingAgreementResponse.StatusCode);
    }

    [Fact]
    public async Task GetBalance_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await Client.GetAsync($"{Endpoint}/{Guid.NewGuid()}/balance");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBalance_AsStudent_ShouldReturnForbidden()
    {
        await RegisterAndConfirmStudentAsync("balance-student@test.pl", Password);
        var student = await LoginAsync("balance-student@test.pl", Password);

        var response = await GetBalanceAsync(
            student.Login.AccessToken,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<HttpResponseMessage> GetBalanceAsync(
        string accessToken,
        Guid tutoringAgreementId)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"{Endpoint}/{tutoringAgreementId}/balance",
            accessToken);

        return await Client.SendAsync(request);
    }

    private async Task<Guid> CreateAgreementWithoutBillingAccountAsync(Guid tutorId)
    {
        return await ExecuteDbAsync(async db =>
        {
            var now = DateTimeOffset.UtcNow;
            var student = new Student(
                new StudentDisplayName($"Balance Student {Guid.NewGuid():N}"),
                now);
            var agreement = new TutoringAgreement(
                new TutorId(tutorId),
                student.Id,
                new Subject("Mathematics"),
                new HourlyRate(new Money(100m, new Currency("PLN"))),
                new AgreementTitle("Balance test agreement"),
                now);

            db.Students.Add(student);
            db.TutoringAgreements.Add(agreement);
            await db.SaveChangesAsync();

            return agreement.Id.Value;
        });
    }

    private async Task<ChargeSet> CreateChargeSetAsync(
        Guid tutorId,
        IReadOnlyList<decimal> amounts,
        DateTimeOffset firstLessonStartsAtUtc,
        int paidChargeCount = 0)
    {
        return await ExecuteDbAsync(async db =>
        {
            var now = DateTimeOffset.UtcNow;
            var student = new Student(
                new StudentDisplayName($"Balance Student {Guid.NewGuid():N}"),
                now);
            var agreement = new TutoringAgreement(
                new TutorId(tutorId),
                student.Id,
                new Subject("Mathematics"),
                new HourlyRate(new Money(100m, new Currency("PLN"))),
                new AgreementTitle("Balance test agreement"),
                now);
            var account = new BillingAccount(agreement.Id, now);
            var lessons = new List<Lesson>();
            var chargeIds = new List<LessonChargeId>();

            db.Students.Add(student);
            db.TutoringAgreements.Add(agreement);

            for (var index = 0; index < amounts.Count; index++)
            {
                var startsAtUtc = firstLessonStartsAtUtc.AddHours(index * 2);
                var lesson = new Lesson(
                    agreement.Id,
                    new TimeSlot(startsAtUtc, startsAtUtc.AddHours(1)),
                    now);
                var charge = account.AddLessonCharge(
                    lesson.Id,
                    new Money(amounts[index], new Currency("PLN")),
                    now);

                lessons.Add(lesson);
                chargeIds.Add(charge.Id);
            }

            if (paidChargeCount > 0)
            {
                account.MarkChargesAsPaid(
                    chargeIds.Take(paidChargeCount).ToArray(),
                    now,
                    null);
            }

            db.BillingAccounts.Add(account);
            db.Lessons.AddRange(lessons);

            await db.SaveChangesAsync();

            return new ChargeSet(
                agreement.Id.Value,
                account.Id.Value,
                chargeIds.Select(chargeId => chargeId.Value).ToArray());
        });
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
            .Where(tutor => tutor.Account.Email.Value == email)
            .Select(tutor => tutor.Id.Value)
            .SingleAsync());

        return new TestTutor(tutorId, session.Login.AccessToken);
    }

    private sealed record ChargeSet(
        Guid TutoringAgreementId,
        Guid BillingAccountId,
        IReadOnlyList<Guid> ChargeIds);

    private sealed record TestTutor(Guid TutorId, string AccessToken);
}
