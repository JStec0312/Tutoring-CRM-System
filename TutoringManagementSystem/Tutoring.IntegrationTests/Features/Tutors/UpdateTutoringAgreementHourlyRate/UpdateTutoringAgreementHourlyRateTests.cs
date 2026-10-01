using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.UpdateTutoringAgreementHourlyRate;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdateTutoringAgreementHourlyRateTests(
    IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/tutoring-agreements";

    [Fact]
    public async Task UpdateHourlyRate_ShouldSetRateAndPersistIt()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var agreementId = await CreateAgreementAsync(tutor.Email, await CreateStudentIdAsync());

        var response = await UpdateAsync(agreementId, tutor.AccessToken, 125);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertRateAsync(agreementId, 125);
    }

    [Fact]
    public async Task UpdateHourlyRate_ShouldChangeExistingRate()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var agreementId = await CreateAgreementAsync(tutor.Email, await CreateStudentIdAsync(), 100);

        var response = await UpdateAsync(agreementId, tutor.AccessToken, 175);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertRateAsync(agreementId, 175);
    }

    [Fact]
    public async Task UpdateHourlyRate_WithNull_ShouldClearRate()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var agreementId = await CreateAgreementAsync(tutor.Email, await CreateStudentIdAsync(), 100);

        var response = await UpdateAsync(agreementId, tutor.AccessToken, null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertRateAsync(agreementId, null);
    }

    [Fact]
    public async Task UpdateHourlyRate_WithZero_ShouldPersistZero()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var agreementId = await CreateAgreementAsync(tutor.Email, await CreateStudentIdAsync());

        var response = await UpdateAsync(agreementId, tutor.AccessToken, 0);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertRateAsync(agreementId, 0);
    }

    [Fact]
    public async Task UpdateHourlyRate_WithNegativeRate_ShouldReturnInvalidRequestAndNotChangeDatabase()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var agreementId = await CreateAgreementAsync(tutor.Email, await CreateStudentIdAsync(), 100);

        var response = await UpdateAsync(agreementId, tutor.AccessToken, -1);

        await AssertInvalidRequestAsync(response);
        await AssertRateAsync(agreementId, 100);
    }

    [Fact]
    public async Task UpdateHourlyRate_WhenAgreementIsMissing_ShouldReturnNotFoundAndNotChangeDatabase()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");

        var response = await UpdateAsync(Guid.NewGuid(), tutor.AccessToken, 125);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateHourlyRate_WhenAgreementBelongsToAnotherTutor_ShouldReturnNotFoundAndNotChangeDatabase()
    {
        var owner = await CreateTutorAsync("owner@test.pl", "owner");
        var otherTutor = await CreateTutorAsync("other@test.pl", "other");
        var agreementId = await CreateAgreementAsync(owner.Email, await CreateStudentIdAsync(), 100);

        var response = await UpdateAsync(agreementId, otherTutor.AccessToken, 125);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertRateAsync(agreementId, 100);
    }

    [Fact]
    public async Task UpdateHourlyRate_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await Client.PutAsJsonAsync(
            $"{Endpoint}/{Guid.NewGuid()}/hourly-rate", new { HourlyRate = 125 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateHourlyRate_AsStudent_ShouldReturnForbidden()
    {
        var student = await CreateStudentAsync("student@test.pl", "student");

        var response = await UpdateAsync(Guid.NewGuid(), student.AccessToken, 125);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateHourlyRate_WhenAgreementEnded_ShouldReturnNotFoundAndNotChangeDatabase()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var agreementId = await CreateAgreementAsync(tutor.Email, await CreateStudentIdAsync(), 100);
        await EndAgreementAsync(agreementId);

        var response = await UpdateAsync(agreementId, tutor.AccessToken, 125);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertRateAsync(agreementId, 100);
    }

    [Fact]
    public async Task UpdateHourlyRate_WithMultipleAgreements_ShouldOnlyUpdateSelectedAgreement()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var studentId = await CreateStudentIdAsync();
        var first = await CreateAgreementAsync(tutor.Email, studentId, 100);
        var second = await CreateAgreementAsync(tutor.Email, studentId, 200);

        var response = await UpdateAsync(first, tutor.AccessToken, 150);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertRateAsync(first, 150);
        await AssertRateAsync(second, 200);
    }

    private async Task<HttpResponseMessage> UpdateAsync(Guid id, string token, decimal? rate)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Put, $"{Endpoint}/{id}/hourly-rate", token, new { HourlyRate = rate });
        return await Client.SendAsync(request);
    }

    private async Task AssertRateAsync(Guid id, decimal? expected)
    {
        var rate = await ExecuteDbAsync(db => db.TutoringAgreements.AsNoTracking()
            .Where(agreement => agreement.Id == new TutoringAgreementId(id))
            .Select(agreement => agreement.HourlyRate == null
                ? (decimal?)null
                : agreement.HourlyRate.PricePerHour.Amount)
            .SingleAsync());
        Assert.Equal(expected, rate);
    }

    private static async Task AssertInvalidRequestAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Request.Invalid", problem!.Extensions["code"]?.ToString());
    }

    private async Task<Guid> CreateAgreementAsync(string tutorEmail, Guid studentId, decimal? rate = 100)
    {
        return await ExecuteDbAsync(async db =>
        {
            var tutorId = await db.Tutors.Where(tutor => tutor.Account.Email.Value == tutorEmail)
                .Select(tutor => tutor.Id).SingleAsync();
            var agreement = new TutoringAgreement(tutorId, new StudentId(studentId),
                new Subject("Mathematics"), rate is null ? null : new HourlyRate(new Money(rate.Value, new Currency("PLN"))),
                new AgreementTitle("Test Agreement"), DateTimeOffset.UtcNow);
            db.TutoringAgreements.Add(agreement);
            await db.SaveChangesAsync();
            return agreement.Id.Value;
        });
    }

    private async Task EndAgreementAsync(Guid id) => await ExecuteDbAsync(async db =>
    {
        var agreement = await db.TutoringAgreements.SingleAsync(item => item.Id == new TutoringAgreementId(id));
        db.Entry(agreement).Property(item => item.Status).CurrentValue = AgreementStatus.Ended;
        await db.SaveChangesAsync();
    });

    private async Task<Guid> CreateStudentIdAsync() =>
        (await CreateStudentAsync($"student-{Guid.NewGuid()}@test.pl", Guid.NewGuid().ToString("N"))).StudentId;

    private async Task<TestTutor> CreateTutorAsync(string email, string userName)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register/tutor",
            new { Email = email, UserName = userName, Password, FirstName = "Tutor", LastName = "Test", PhoneNumber = (string?)null });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await ConfirmEmailAsync(await GetVerificationTokenAsync(email));
        var session = await LoginAsync(email, Password);
        var id = await ExecuteDbAsync(db => db.Tutors.Where(tutor => tutor.Account.Email.Value == email).Select(tutor => tutor.Id.Value).SingleAsync());
        return new TestTutor(email, session.Login.AccessToken, id);
    }

    private async Task<TestStudent> CreateStudentAsync(string email, string userName)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register/student",
            new { Email = email, Username = userName, Password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await ConfirmEmailAsync(await GetVerificationTokenAsync(email));
        var session = await LoginAsync(email, Password);
        var id = await ExecuteDbAsync(db => db.Students.Where(student => student.Account!.Email.Value == email).Select(student => student.Id.Value).SingleAsync());
        return new TestStudent(session.Login.AccessToken, id);
    }

    private sealed record TestTutor(string Email, string AccessToken, Guid TutorId);
    private sealed record TestStudent(string AccessToken, Guid StudentId);
}
