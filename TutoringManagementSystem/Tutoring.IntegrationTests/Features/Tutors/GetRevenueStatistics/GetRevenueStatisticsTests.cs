using System.Net;
using System.Net.Http.Json;
using Tutoring.Api.Features.Tutors.GetRevenueStatistics;
using Tutoring.Domain.Billing;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.GetRevenueStatistics;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetRevenueStatisticsTests(IntegrationTestFixture fixture) : LessonIntegrationTestBase(fixture)
{
    private const string Endpoint = "/api/tutors/me/statistics/revenue";
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);
    protected override DateTimeOffset AgreementCreatedAtUtc => Now.AddYears(-1);

    [Fact]
    public async Task SumsOwnedPaymentsByCurrencyAndUtcCalendarPeriod()
    {
        var tutor = await CreateTutorAsync("owner@test.pl", "owner");
        var otherTutor = await CreateTutorAsync("other@test.pl", "other");
        var student = await CreateStudentAsync("student@test.pl", "student");
        var otherStudent = await CreateStudentAsync("other-student@test.pl", "other-student");
        var firstAgreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var secondAgreement = await CreateAgreementAsync(tutor.Email, otherStudent.StudentId);
        var foreignAgreement = await CreateAgreementAsync(otherTutor.Email, student.StudentId);

        var weekStart = new DateTimeOffset(2025, 12, 29, 0, 0, 0, TimeSpan.Zero);
        var yearStart = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        await AddPaymentsAsync(firstAgreement,
            (1000m, "PLN", weekStart.AddTicks(-1)),
            (30m, "PLN", weekStart),
            (100.25m, "PLN", yearStart.ToOffset(TimeSpan.FromHours(-1))),
            (25m, "EUR", Now),
            (1000m, "PLN", Now.AddTicks(1)));
        await AddPaymentsAsync(secondAgreement, (20m, "PLN", Now));
        await AddPaymentsAsync(foreignAgreement, (1000m, "PLN", Now));

        using var factory = Fixture.WithTimeProvider(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var totals = await GetRevenueAsync(client, tutor.AccessToken);

        Assert.Equal(
            [new CurrencyRevenue("EUR", 25m, 25m, 25m), new CurrencyRevenue("PLN", 150.25m, 120.25m, 120.25m)],
            totals.RevenueByCurrency);
    }

    [Fact]
    public async Task SeparatesWeeklyMonthlyAndYearlyAmounts()
    {
        var tutor = await CreateTutorAsync("owner@test.pl", "owner");
        var student = await CreateStudentAsync("student@test.pl", "student");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var now = new DateTimeOffset(2026, 4, 15, 12, 0, 0, TimeSpan.Zero);

        await AddPaymentsAsync(agreement,
            (50m, "PLN", now.AddDays(-20)),
            (30m, "PLN", now.AddDays(-10)),
            (20m, "PLN", now.AddDays(-2)));

        using var factory = Fixture.WithTimeProvider(new FixedTimeProvider(now));
        using var client = factory.CreateClient();
        var totals = await GetRevenueAsync(client, tutor.AccessToken);

        Assert.Equal(new CurrencyRevenue("PLN", 20m, 50m, 100m), Assert.Single(totals.RevenueByCurrency));
    }

    [Fact]
    public async Task TutorWithoutPayments_ReturnsEmptyList()
    {
        var tutor = await CreateTutorAsync("owner@test.pl", "owner");

        Assert.Empty((await GetRevenueAsync(Client, tutor.AccessToken)).RevenueByCurrency);
    }

    [Fact]
    public async Task RequiresTutorRole()
    {
        var student = await CreateStudentAsync("student@test.pl", "student");
        using var anonymous = await Client.GetAsync(Endpoint);
        using var request = CreateAuthorizedRequest(HttpMethod.Get, Endpoint, student.AccessToken);
        using var forbidden = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task FiltersByCurrencyAndIncludesBothRangeBoundaries()
    {
        var tutor = await CreateTutorAsync("owner@test.pl", "owner");
        var student = await CreateStudentAsync("student@test.pl", "student");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var day = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero);

        await AddPaymentsAsync(agreement,
            (40m, "PLN", day.AddTicks(-1)),
            (5m, "PLN", day),
            (10m, "PLN", day.AddDays(1).AddTicks(-1)),
            (20m, "PLN", day.AddDays(1)),
            (30m, "EUR", day));

        using var factory = Fixture.WithTimeProvider(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var totals = await GetRevenueAsync(client, tutor.AccessToken, "?currency=pln&startDate=2025-12-31&endDate=2025-12-31");

        Assert.Equal(new CurrencyRevenue("PLN", 15m, 15m, 15m), Assert.Single(totals.RevenueByCurrency));
        Assert.Empty((await GetRevenueAsync(client, tutor.AccessToken, "?currency=USD")).RevenueByCurrency);
    }

    [Theory]
    [InlineData("?currency=PL")]
    [InlineData("?endDate=invalid")]
    [InlineData("?endDate=9999-12-31")]
    [InlineData("?startDate=invalid")]
    [InlineData("?startDate=2025-12-31&endDate=2025-12-30")]
    public async Task InvalidFilters_ReturnBadRequest(string query)
    {
        var tutor = await CreateTutorAsync("owner@test.pl", "owner");
        using var request = CreateAuthorizedRequest(HttpMethod.Get, Endpoint + query, tutor.AccessToken);
        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<RevenueStatisticsResponse> GetRevenueAsync(HttpClient client, string token, string query = "")
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, Endpoint + query, token);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<RevenueStatisticsResponse>(
            await response.Content.ReadFromJsonAsync<RevenueStatisticsResponse>());
    }

    private Task AddPaymentsAsync(Guid agreementId,
        params (decimal Amount, string Currency, DateTimeOffset PaidAt)[] payments) =>
        ExecuteDbAsync(async db =>
        {
            var account = new BillingAccount(new TutoringAgreementId(agreementId), AgreementCreatedAtUtc);
            db.BillingAccounts.Add(account);
            db.Payments.AddRange(payments.Select(payment =>
                new Payment(account.Id, new Money(payment.Amount, new Currency(payment.Currency)),
                    payment.PaidAt, null)));
            await db.SaveChangesAsync();
        });

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
