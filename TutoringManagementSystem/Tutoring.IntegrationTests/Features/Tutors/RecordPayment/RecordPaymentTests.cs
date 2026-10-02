using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.RecordPayment;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.RecordPayment;

[Collection(IntegrationTestCollection.Name)]
public sealed class RecordPaymentTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/tutoring-agreements";

    [Fact]
    public async Task RecordPayment_ShouldCreatePaymentAndPersistExpectedData()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var agreementId = await CreateAgreementAsync(tutor.Email, await CreateStudentIdAsync());
        var paidAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        var response = await RecordPaymentAsync(
            tutor.AccessToken, agreementId, 125.50m, paidAt, "REF-001");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RecordPaymentResponse>();
        Assert.NotNull(result);
        Assert.Equal(125.50m, result!.Amount);
        Assert.Equal("PLN", result.Currency);
        Assert.Equal(paidAt, result.PaidAtUtc);
        Assert.Equal("REF-001", result.Reference);

        var payment = await ExecuteDbAsync(db => db.Payments
            .AsNoTracking()
            .SingleAsync());
        var account = await ExecuteDbAsync(db => db.BillingAccounts
            .AsNoTracking()
            .SingleAsync());

        Assert.Equal(result.PaymentId, payment.Id.Value);
        Assert.Equal(account.Id, payment.BillingAccountId);
        Assert.Equal(new TutoringAgreementId(agreementId), account.TutoringAgreementId);
        Assert.Equal(125.50m, payment.Amount.Amount);
        Assert.Equal("PLN", payment.Amount.Currency.Code);
        Assert.Equal(paidAt, payment.PaidAtUtc);
        Assert.Equal("REF-001", payment.Reference!.Value);
    }

    [Fact]
    public async Task RecordPayment_ShouldUseExistingBillingAccount()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var agreementId = await CreateAgreementAsync(tutor.Email, await CreateStudentIdAsync());
        var billingAccountId = await CreateBillingAccountAsync(agreementId);

        var response = await RecordPaymentAsync(
            tutor.AccessToken, agreementId, 80, DateTimeOffset.UtcNow, null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var account = await ExecuteDbAsync(db => db.BillingAccounts
            .AsNoTracking()
            .SingleAsync());

        Assert.Equal(billingAccountId, account.Id.Value);
        Assert.Equal(1, await ExecuteDbAsync(db => db.BillingAccounts.CountAsync()));
        Assert.Equal(1, await ExecuteDbAsync(db => db.Payments.CountAsync()));
    }

    [Fact]
    public async Task RecordPayment_WithoutBillingAccount_ShouldCreateOne()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var agreementId = await CreateAgreementAsync(tutor.Email, await CreateStudentIdAsync());

        var response = await RecordPaymentAsync(
            tutor.AccessToken, agreementId, 80, DateTimeOffset.UtcNow, null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, await ExecuteDbAsync(db => db.BillingAccounts.CountAsync()));
        Assert.Equal(1, await ExecuteDbAsync(db => db.Payments.CountAsync()));
    }

    [Fact]
    public async Task RecordPayment_ShouldAllowMultiplePaymentsForOneBillingAccount()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var agreementId = await CreateAgreementAsync(tutor.Email, await CreateStudentIdAsync());

        var first = await RecordPaymentAsync(
            tutor.AccessToken, agreementId, 80, DateTimeOffset.UtcNow, "FIRST");
        var second = await RecordPaymentAsync(
            tutor.AccessToken, agreementId, 20, DateTimeOffset.UtcNow, null);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(1, await ExecuteDbAsync(db => db.BillingAccounts.CountAsync()));
        Assert.Equal(2, await ExecuteDbAsync(db => db.Payments.CountAsync()));
        Assert.Equal(
            new[] { null, "FIRST" },
            await ExecuteDbAsync(db => db.Payments
                .AsNoTracking()
                .OrderBy(payment => payment.Amount.Amount)
                .Select(payment => payment.Reference == null
                    ? null
                    : payment.Reference.Value)
                .ToArrayAsync()));
    }

    [Theory]
    [InlineData(0, HttpStatusCode.UnprocessableEntity, "Billing.PaymentNotPositive")]
    [InlineData(-1, HttpStatusCode.BadRequest, "Request.Invalid")]
    public async Task RecordPayment_WithNonPositiveAmount_ShouldReturnValidationErrorAndNotPersist(
        decimal amount,
        HttpStatusCode expectedStatusCode,
        string expectedProblemCode)
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var agreementId = await CreateAgreementAsync(tutor.Email, await CreateStudentIdAsync());

        var response = await RecordPaymentAsync(
            tutor.AccessToken, agreementId, amount, DateTimeOffset.UtcNow, null);

        Assert.Equal(expectedStatusCode, response.StatusCode);
        await AssertProblemCodeAsync(response, expectedProblemCode);
        await AssertNoBillingDataAsync();
    }

    [Fact]
    public async Task RecordPayment_WhenAgreementDoesNotExist_ShouldReturnNotFoundAndNotPersist()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");

        var response = await RecordPaymentAsync(
            tutor.AccessToken, Guid.NewGuid(), 50, DateTimeOffset.UtcNow, null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, "TutoringAgreements.NotFound");
        await AssertNoBillingDataAsync();
    }

    [Fact]
    public async Task RecordPayment_WhenAgreementBelongsToAnotherTutor_ShouldReturnNotFoundAndNotPersist()
    {
        var owner = await CreateTutorAsync("owner@test.pl", "owner");
        var otherTutor = await CreateTutorAsync("other@test.pl", "other");
        var agreementId = await CreateAgreementAsync(owner.Email, await CreateStudentIdAsync());

        var response = await RecordPaymentAsync(
            otherTutor.AccessToken, agreementId, 50, DateTimeOffset.UtcNow, null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, "TutoringAgreements.NotFound");
        await AssertNoBillingDataAsync();
    }

    [Fact]
    public async Task RecordPayment_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            $"{Endpoint}/{Guid.NewGuid()}/payments",
            new { Amount = 50, PaidAtUtc = DateTimeOffset.UtcNow, Reference = (string?)null });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertNoBillingDataAsync();
    }

    [Fact]
    public async Task RecordPayment_AsStudent_ShouldReturnForbidden()
    {
        var student = await CreateStudentAsync("student@test.pl", "student");

        var response = await RecordPaymentAsync(
            student.AccessToken, Guid.NewGuid(), 50, DateTimeOffset.UtcNow, null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertNoBillingDataAsync();
    }

    private async Task<HttpResponseMessage> RecordPaymentAsync(
        string accessToken,
        Guid agreementId,
        decimal amount,
        DateTimeOffset paidAtUtc,
        string? reference)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"{Endpoint}/{agreementId}/payments",
            accessToken,
            new { Amount = amount, PaidAtUtc = paidAtUtc, Reference = reference });
        return await Client.SendAsync(request);
    }

    private async Task AssertNoBillingDataAsync()
    {
        Assert.Equal(0, await ExecuteDbAsync(db => db.Payments.CountAsync()));
        Assert.Equal(0, await ExecuteDbAsync(db => db.BillingAccounts.CountAsync()));
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem!.Extensions.TryGetValue("code", out var code));
        Assert.Equal(expectedCode, code?.ToString());
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
                FirstName = "Tutor",
                LastName = "Test",
                PhoneNumber = (string?)null
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await ConfirmEmailAsync(await GetVerificationTokenAsync(email));
        return new TestTutor(email, (await LoginAsync(email, Password)).Login.AccessToken);
    }

    private async Task<TestStudent> CreateStudentAsync(string email, string userName)
    {
        await RegisterAndConfirmStudentAsync(email, Password);
        var accessToken = (await LoginAsync(email, Password)).Login.AccessToken;
        var studentId = await ExecuteDbAsync(db => db.Students
            .Where(student => student.Account!.Email.Value == email)
            .Select(student => student.Id.Value)
            .SingleAsync());
        return new TestStudent(accessToken, studentId);
    }

    private async Task<Guid> CreateStudentIdAsync()
    {
        return (await CreateStudentAsync(
            $"student-{Guid.NewGuid():N}@test.pl",
            $"student-{Guid.NewGuid():N}")).StudentId;
    }

    private async Task<Guid> CreateAgreementAsync(string tutorEmail, Guid studentId)
    {
        return await ExecuteDbAsync(async db =>
        {
            var tutorId = await db.Tutors
                .Where(tutor => tutor.Account.Email.Value == tutorEmail)
                .Select(tutor => tutor.Id)
                .SingleAsync();
            var agreement = new TutoringAgreement(
                tutorId,
                new StudentId(studentId),
                new Subject("Mathematics"),
                new HourlyRate(new Money(100, new Currency("PLN"))),
                new AgreementTitle("Test Agreement"),
                DateTimeOffset.UtcNow);
            db.TutoringAgreements.Add(agreement);
            await db.SaveChangesAsync();
            return agreement.Id.Value;
        });
    }

    private Task<Guid> CreateBillingAccountAsync(Guid agreementId)
    {
        return ExecuteDbAsync(async db =>
        {
            var account = new BillingAccount(
                new TutoringAgreementId(agreementId),
                DateTimeOffset.UtcNow);
            db.BillingAccounts.Add(account);
            await db.SaveChangesAsync();
            return account.Id.Value;
        });
    }

    private sealed record TestTutor(string Email, string AccessToken);

    private sealed record TestStudent(string AccessToken, Guid StudentId);
}
