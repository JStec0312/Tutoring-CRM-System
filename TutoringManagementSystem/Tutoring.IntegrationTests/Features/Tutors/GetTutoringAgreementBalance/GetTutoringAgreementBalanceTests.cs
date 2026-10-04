using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.GetTutoringAgreementBalance;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
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
    public async Task GetBalance_WithoutBillingAccount_ShouldReturnZeros()
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();

        var response = await GetBalanceAsync(tutor.AccessToken, agreementId);

        var result = await AssertSuccessfulResponseAsync(response, agreementId);

        Assert.Equal(0m, result.TotalCharged);
        Assert.Equal(0m, result.TotalPaid);
        Assert.Equal(0m, result.Balance);
    }

    [Fact]
    public async Task GetBalance_WithActiveChargesAndNoPayments_ShouldReturnTotalCharged()
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        await CreateBillingDataAsync(agreementId, [125.50m, 74.50m], []);

        var response = await GetBalanceAsync(tutor.AccessToken, agreementId);

        var result = await AssertSuccessfulResponseAsync(response, agreementId);

        Assert.Equal(200m, result.TotalCharged);
        Assert.Equal(0m, result.TotalPaid);
        Assert.Equal(200m, result.Balance);
    }

    [Fact]
    public async Task GetBalance_WithMultiplePayments_ShouldReturnTotalPaidAndCalculatedBalance()
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        await CreateBillingDataAsync(agreementId, [200m], [80m, 45.50m]);

        var response = await GetBalanceAsync(tutor.AccessToken, agreementId);

        var result = await AssertSuccessfulResponseAsync(response, agreementId);

        Assert.Equal(200m, result.TotalCharged);
        Assert.Equal(125.50m, result.TotalPaid);
        Assert.Equal(74.50m, result.Balance);
    }

    [Fact]
    public async Task GetBalance_WithMultipleLessonCharges_ShouldSumActiveCharges()
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        await CreateBillingDataAsync(agreementId, [100m, 150m, 37.25m], []);

        var response = await GetBalanceAsync(tutor.AccessToken, agreementId);

        var result = await AssertSuccessfulResponseAsync(response, agreementId);

        Assert.Equal(287.25m, result.TotalCharged);
        Assert.Equal(0m, result.TotalPaid);
        Assert.Equal(287.25m, result.Balance);
    }

    [Fact]
    public async Task GetBalance_ShouldIgnoreNonActiveLessonCharges()
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        await CreateBillingDataAsync(
            agreementId,
            [100m, 75m],
            [],
            inactiveChargeAmount: 500m);

        var response = await GetBalanceAsync(tutor.AccessToken, agreementId);

        var result = await AssertSuccessfulResponseAsync(response, agreementId);

        Assert.Equal(175m, result.TotalCharged);
        Assert.Equal(0m, result.TotalPaid);
        Assert.Equal(175m, result.Balance);
    }

    [Fact]
    public async Task GetBalance_WhenPaymentsExceedCharges_ShouldReturnNegativeBalance()
    {
        var (tutor, agreementId) = await CreateTutorAndAgreementAsync();
        await CreateBillingDataAsync(agreementId, [50m], [80m, 25m]);

        var response = await GetBalanceAsync(tutor.AccessToken, agreementId);

        var result = await AssertSuccessfulResponseAsync(response, agreementId);

        Assert.Equal(50m, result.TotalCharged);
        Assert.Equal(105m, result.TotalPaid);
        Assert.Equal(-55m, result.Balance);
    }

    [Fact]
    public async Task GetBalance_WhenAgreementDoesNotExist_ShouldReturnNotFound()
    {
        var tutor = await CreateTutorAsync(
            $"tutor-{Guid.NewGuid():N}@test.pl",
            $"tutor-{Guid.NewGuid():N}");

        var response = await GetBalanceAsync(tutor.AccessToken, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, "TutoringAgreements.NotFound");
    }

    [Fact]
    public async Task GetBalance_WhenAgreementBelongsToAnotherTutor_ShouldReturnNotFound()
    {
        var owner = await CreateTutorAsync(
            $"owner-{Guid.NewGuid():N}@test.pl",
            $"owner-{Guid.NewGuid():N}");
        var otherTutor = await CreateTutorAsync(
            $"other-{Guid.NewGuid():N}@test.pl",
            $"other-{Guid.NewGuid():N}");
        var agreementId = await CreateAgreementAsync(
            owner.Email,
            await CreateStudentIdAsync());

        var response = await GetBalanceAsync(
            otherTutor.AccessToken,
            agreementId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, "TutoringAgreements.NotFound");
    }

    [Fact]
    public async Task GetBalance_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await Client.GetAsync(
            $"{Endpoint}/{Guid.NewGuid()}/balance");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBalance_AsStudent_ShouldReturnForbidden()
    {
        var student = await CreateStudentAsync(
            $"student-{Guid.NewGuid():N}@test.pl",
            $"student-{Guid.NewGuid():N}");

        var response = await GetBalanceAsync(
            student.AccessToken,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<GetTutoringAgreementBalanceResponse>
        AssertSuccessfulResponseAsync(
            HttpResponseMessage response,
            Guid agreementId)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<
                GetTutoringAgreementBalanceResponse>();

        Assert.NotNull(result);
        Assert.Equal(agreementId, result!.TutoringAgreementId);
        Assert.Equal("PLN", result.Currency);

        return result;
    }

    private Task<HttpResponseMessage> GetBalanceAsync(
        string accessToken,
        Guid agreementId)
    {
        return Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"{Endpoint}/{agreementId}/balance",
                accessToken));
    }

    private async Task CreateBillingDataAsync(
        Guid agreementId,
        decimal[] activeChargeAmounts,
        decimal[] paymentAmounts,
        decimal? inactiveChargeAmount = null)
    {
        await ExecuteDbAsync(async db =>
        {
            var account = new BillingAccount(
                new TutoringAgreementId(agreementId),
                DateTimeOffset.UtcNow);

            var lessons = new List<Lesson>();
            foreach (var amount in activeChargeAmounts)
            {
                var lesson = CreateLesson(agreementId, lessons.Count);
                lessons.Add(lesson);
                db.Lessons.Add(lesson);
            }

            if (inactiveChargeAmount.HasValue)
            {
                var lesson = CreateLesson(agreementId, lessons.Count);
                lessons.Add(lesson);
                db.Lessons.Add(lesson);
            }

            await db.SaveChangesAsync();

            for (var index = 0; index < activeChargeAmounts.Length; index++)
            {
                account.AddLessonCharge(
                    lessons[index].Id,
                    new Money(activeChargeAmounts[index], new Currency("PLN")),
                    DateTimeOffset.UtcNow);
            }

            if (inactiveChargeAmount.HasValue)
            {
                account.AddLessonCharge(
                    lessons[^1].Id,
                    new Money(inactiveChargeAmount.Value, new Currency("PLN")),
                    DateTimeOffset.UtcNow);
            }

            foreach (var amount in paymentAmounts)
            {
                account.RecordPayment(
                    new Money(amount, new Currency("PLN")),
                    DateTimeOffset.UtcNow,
                    null);
            }

            db.BillingAccounts.Add(account);
            await db.SaveChangesAsync();

            if (inactiveChargeAmount.HasValue)
            {
                await db.LessonCharges
                    .Where(charge =>
                        charge.BillingAccountId == account.Id &&
                        charge.Amount.Amount == inactiveChargeAmount.Value)
                    .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(
                            charge => charge.Status,
                            ChargeStatus.Cancelled));
            }
        });
    }

    private static Lesson CreateLesson(Guid agreementId, int index)
    {
        var startsAtUtc = DateTimeOffset.UtcNow
            .AddDays(-index - 1)
            .AddHours(-1);

        return new Lesson(
            new TutoringAgreementId(agreementId),
            new TimeSlot(startsAtUtc, startsAtUtc.AddHours(1)),
            DateTimeOffset.UtcNow);
    }

    private async Task<(TestTutor Tutor, Guid AgreementId)>
        CreateTutorAndAgreementAsync()
    {
        var tutor = await CreateTutorAsync(
            $"tutor-{Guid.NewGuid():N}@test.pl",
            $"tutor-{Guid.NewGuid():N}");
        var agreementId = await CreateAgreementAsync(
            tutor.Email,
            await CreateStudentIdAsync());

        return (tutor, agreementId);
    }

    private async Task<TestTutor> CreateTutorAsync(
        string email,
        string userName)
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

        return new TestTutor(
            email,
            (await LoginAsync(email, Password)).Login.AccessToken);
    }

    private async Task<TestStudent> CreateStudentAsync(
        string email,
        string userName)
    {
        await RegisterAndConfirmStudentAsync(email, Password);
        var accessToken = (await LoginAsync(email, Password)).Login.AccessToken;
        var studentId = await ExecuteDbAsync(db =>
            db.Students
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

    private Task<Guid> CreateAgreementAsync(
        string tutorEmail,
        Guid studentId)
    {
        return ExecuteDbAsync(async db =>
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

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.True(problem!.Extensions.TryGetValue("code", out var code));
        Assert.Equal(expectedCode, code?.ToString());
    }

    private sealed record TestTutor(
        string Email,
        string AccessToken);

    private sealed record TestStudent(
        string AccessToken,
        Guid StudentId);
}
