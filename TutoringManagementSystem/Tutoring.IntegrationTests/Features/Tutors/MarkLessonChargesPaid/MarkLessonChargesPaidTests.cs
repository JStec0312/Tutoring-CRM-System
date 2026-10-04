using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Domain.Tutors;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.MarkLessonChargesPaid;

[Collection(IntegrationTestCollection.Name)]
public sealed class MarkLessonChargesPaidTests(
    IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/me/lesson-charges";

    [Fact]
    public async Task MarkSingleChargeAsPaid_ShouldPersistPaymentAndChargeState()
    {
        var tutor = await CreateTutorAsync("single-payment@test.pl", "single-payment");
        var data = await CreateChargeSetAsync(tutor.TutorId, 75.50m);
        var paidAt = DateTimeOffset.UtcNow;

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"{Endpoint}/{data.ChargeIds[0]}/mark-paid",
            tutor.AccessToken,
            new { PaidAtUtc = paidAt, Reference = "TRANSFER-001" });

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var result = await ExecuteDbAsync(async db => new
        {
            Charge = await db.LessonCharges.AsNoTracking()
                .Where(charge => charge.Id == new LessonChargeId(data.ChargeIds[0]))
                .Select(charge => new { charge.IsPaid, charge.PaidAtUtc })
                .SingleAsync(),
            Payment = await db.Payments.AsNoTracking()
                .Where(payment => payment.BillingAccountId == new BillingAccountId(data.BillingAccountId))
                .Select(payment => new
                {
                    Amount = payment.Amount.Amount,
                    payment.PaidAtUtc,
                    Reference = payment.Reference!.Value
                })
                .SingleAsync()
        });

        Assert.True(result.Charge.IsPaid);
        Assert.Equal(paidAt, result.Charge.PaidAtUtc);
        Assert.Equal(75.50m, result.Payment.Amount);
        Assert.Equal(paidAt, result.Payment.PaidAtUtc);
        Assert.Equal("TRANSFER-001", result.Payment.Reference);
    }

    [Fact]
    public async Task MarkMultipleChargesAsPaid_ShouldCreateOnePaymentForTheirTotal()
    {
        var tutor = await CreateTutorAsync("bulk-payment@test.pl", "bulk-payment");
        var data = await CreateChargeSetAsync(tutor.TutorId, 25m, 50.50m);
        var paidAt = DateTimeOffset.UtcNow;

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"{Endpoint}/mark-paid",
            tutor.AccessToken,
            new
            {
                LessonChargeIds = data.ChargeIds,
                PaidAtUtc = paidAt,
                Reference = "BULK-TRANSFER-001"
            });

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var result = await ExecuteDbAsync(async db => new
        {
            Charges = await db.LessonCharges.AsNoTracking()
                .Where(charge => data.ChargeIds.Contains(charge.Id.Value))
                .Select(charge => new { charge.IsPaid, charge.PaidAtUtc })
                .ToListAsync(),
            Payments = await db.Payments.AsNoTracking()
                .Where(payment => payment.BillingAccountId == new BillingAccountId(data.BillingAccountId))
                .Select(payment => new { Amount = payment.Amount.Amount, payment.Reference!.Value })
                .ToListAsync()
        });

        Assert.Equal(2, result.Charges.Count);
        Assert.All(result.Charges, charge =>
        {
            Assert.True(charge.IsPaid);
            Assert.Equal(paidAt, charge.PaidAtUtc);
        });
        var payment = Assert.Single(result.Payments);
        Assert.Equal(75.50m, payment.Amount);
        Assert.Equal("BULK-TRANSFER-001", payment.Value);
    }

    [Fact]
    public async Task MarkMultipleChargesFromDifferentAccounts_ShouldRejectWithoutChangingCharges()
    {
        var tutor = await CreateTutorAsync("different-accounts@test.pl", "different-accounts");
        var first = await CreateChargeSetAsync(tutor.TutorId, 25m);
        var second = await CreateChargeSetAsync(tutor.TutorId, 50m);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"{Endpoint}/mark-paid",
            tutor.AccessToken,
            new
            {
                LessonChargeIds = new[] { first.ChargeIds[0], second.ChargeIds[0] },
                PaidAtUtc = DateTimeOffset.UtcNow
            });

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var paidStates = await ExecuteDbAsync(db => db.LessonCharges.AsNoTracking()
            .Where(charge => new[] { first.ChargeIds[0], second.ChargeIds[0] }.Contains(charge.Id.Value))
            .Select(charge => charge.IsPaid)
            .ToListAsync());
        Assert.Equal(new[] { false, false }, paidStates);
    }

    [Fact]
    public async Task MarkAlreadyPaidCharge_ShouldRejectAndNotCreateAnotherPayment()
    {
        var tutor = await CreateTutorAsync("already-paid@test.pl", "already-paid");
        var data = await CreateChargeSetAsync(tutor.TutorId, 25m);
        var body = new { PaidAtUtc = DateTimeOffset.UtcNow };

        using (var firstRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"{Endpoint}/{data.ChargeIds[0]}/mark-paid",
            tutor.AccessToken,
            body))
        {
            Assert.Equal(HttpStatusCode.NoContent, (await Client.SendAsync(firstRequest)).StatusCode);
        }

        using var secondRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"{Endpoint}/{data.ChargeIds[0]}/mark-paid",
            tutor.AccessToken,
            body);
        var response = await Client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var paymentCount = await ExecuteDbAsync(db => db.Payments.CountAsync(
            payment => payment.BillingAccountId == new BillingAccountId(data.BillingAccountId)));
        Assert.Equal(1, paymentCount);
    }

    [Fact]
    public async Task MarkChargeOwnedByAnotherTutor_ShouldReturnNotFound()
    {
        var owner = await CreateTutorAsync("charge-owner@test.pl", "charge-owner");
        var otherTutor = await CreateTutorAsync("charge-other@test.pl", "charge-other");
        var data = await CreateChargeSetAsync(owner.TutorId, 25m);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"{Endpoint}/{data.ChargeIds[0]}/mark-paid",
            otherTutor.AccessToken,
            new { PaidAtUtc = DateTimeOffset.UtcNow });

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var isPaid = await ExecuteDbAsync(db => db.LessonCharges.AsNoTracking()
            .Where(charge => charge.Id == new LessonChargeId(data.ChargeIds[0]))
            .Select(charge => charge.IsPaid)
            .SingleAsync());
        Assert.False(isPaid);
    }

    [Fact]
    public async Task MarkChargeWithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            $"{Endpoint}/{Guid.NewGuid()}/mark-paid",
            new { PaidAtUtc = DateTimeOffset.UtcNow });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<ChargeSet> CreateChargeSetAsync(Guid tutorId, params decimal[] amounts)
    {
        return await ExecuteDbAsync(async db =>
        {
            var now = DateTimeOffset.UtcNow;
            var student = new Student(new StudentDisplayName($"Test Student {Guid.NewGuid():N}"), now);
            var agreement = new TutoringAgreement(
                new TutorId(tutorId),
                student.Id,
                new Subject("Mathematics"),
                new HourlyRate(new Money(100m, new Currency("PLN"))),
                new AgreementTitle("Payment test agreement"),
                now);
            var account = new BillingAccount(agreement.Id, now);
            var chargeIds = new List<Guid>();

            db.Students.Add(student);
            db.TutoringAgreements.Add(agreement);

            foreach (var amount in amounts)
            {
                var lesson = new Lesson(
                    agreement.Id,
                    new TimeSlot(now.AddHours(-3), now.AddHours(-2)),
                    now);
                var charge = account.AddLessonCharge(
                    lesson.Id,
                    new Money(amount, new Currency("PLN")),
                    now);
                db.Lessons.Add(lesson);
                chargeIds.Add(charge.Id.Value);
            }

            db.BillingAccounts.Add(account);
            await db.SaveChangesAsync();
            return new ChargeSet(account.Id.Value, chargeIds);
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

    private sealed record ChargeSet(Guid BillingAccountId, IReadOnlyList<Guid> ChargeIds);
    private sealed record TestTutor(Guid TutorId, string AccessToken);
}

