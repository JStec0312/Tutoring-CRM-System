using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.AddStudentManually;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.UpdateStudentHourlyRate;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdateStudentHourlyRateTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/students";

    [Fact]
    public async Task UpdateStudentHourlyRate_ShouldUpdateHourlyRate()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await AddStudentAsync(tutor.AccessToken);

        var response = await UpdateHourlyRateAsync(
            tutor.AccessToken,
            student.StudentId,
            120m);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var agreement = await GetAgreementAsync(student.AgreementId);
        Assert.NotNull(agreement.HourlyRate);
        Assert.Equal(120m, agreement.HourlyRate!.PricePerHour.Amount);
        Assert.Equal("PLN", agreement.HourlyRate.PricePerHour.Currency.Code);
    } 

    [Fact]
    public async Task UpdateStudentHourlyRate_ShouldAllowChangingExistingHourlyRate()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await AddStudentAsync(tutor.AccessToken, 80m);

        var response = await UpdateHourlyRateAsync(
            tutor.AccessToken,
            student.StudentId,
            120m);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var agreement = await GetAgreementAsync(student.AgreementId);
        Assert.NotNull(agreement.HourlyRate);
        Assert.Equal(120m, agreement.HourlyRate!.PricePerHour.Amount);
        Assert.Equal("PLN", agreement.HourlyRate.PricePerHour.Currency.Code);
    }

    [Fact]
    public async Task UpdateStudentHourlyRate_WithNull_ShouldClearHourlyRate()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await AddStudentAsync(tutor.AccessToken, 80m);

        var response = await UpdateHourlyRateAsync(
            tutor.AccessToken,
            student.StudentId,
            null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var agreement = await GetAgreementAsync(student.AgreementId);
        Assert.Null(agreement.HourlyRate);
    }

    [Fact]
    public async Task UpdateStudentHourlyRate_WithNegativeRate_ShouldReturnBadRequestAndNotModifyAgreement()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await AddStudentAsync(tutor.AccessToken, 80m);

        var response = await UpdateHourlyRateAsync(
            tutor.AccessToken,
            student.StudentId,
            -1m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, "Request.Invalid");

        var agreement = await GetAgreementAsync(student.AgreementId);
        Assert.NotNull(agreement.HourlyRate);
        Assert.Equal(80m, agreement.HourlyRate!.PricePerHour.Amount);
        Assert.Equal("PLN", agreement.HourlyRate.PricePerHour.Currency.Code);
    }

    [Fact]
    public async Task UpdateStudentHourlyRate_WhenStudentDoesNotExist_ShouldReturnNotFound()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");

        var response = await UpdateHourlyRateAsync(
            tutor.AccessToken,
            Guid.NewGuid(),
            120m);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, "TutoringAgreements.NotFound");
    }

    [Fact]
    public async Task UpdateStudentHourlyRate_WhenAgreementBelongsToAnotherTutor_ShouldReturnNotFoundAndNotModifyAgreement()
    {
        var owner = await CreateTutorAsync("owner@test.pl", "owner");
        var otherTutor = await CreateTutorAsync("other@test.pl", "other");
        var student = await AddStudentAsync(owner.AccessToken, 80m);

        var response = await UpdateHourlyRateAsync(
            otherTutor.AccessToken,
            student.StudentId,
            120m);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, "TutoringAgreements.NotFound");

        var agreement = await GetAgreementAsync(student.AgreementId);
        Assert.NotNull(agreement.HourlyRate);
        Assert.Equal(80m, agreement.HourlyRate!.PricePerHour.Amount);
        Assert.Equal("PLN", agreement.HourlyRate.PricePerHour.Currency.Code);
    }

    [Fact]
    public async Task UpdateStudentHourlyRate_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{Endpoint}/{Guid.NewGuid()}/hourly-rate")
        {
            Content = JsonContent.Create(new { HourlyRate = 120m })
        };

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStudentHourlyRate_AsStudent_ShouldReturnForbidden()
    {
        var student = await CreateStudentAsync("student@test.pl", "student");

        var response = await UpdateHourlyRateAsync(
            student.AccessToken,
            Guid.NewGuid(),
            120m);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStudentHourlyRate_WithZero_ShouldSetZeroHourlyRate()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await AddStudentAsync(tutor.AccessToken);

        var response = await UpdateHourlyRateAsync(
            tutor.AccessToken,
            student.StudentId,
            0m);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var agreement = await GetAgreementAsync(student.AgreementId);
        Assert.NotNull(agreement.HourlyRate);
        Assert.Equal(0m, agreement.HourlyRate!.PricePerHour.Amount);
        Assert.Equal("PLN", agreement.HourlyRate.PricePerHour.Currency.Code);
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
        var accessToken = (await LoginAsync(email, Password)).Login.AccessToken;
        return new TestTutor(accessToken);
    }

    private async Task<TestStudent> CreateStudentAsync(
        string email,
        string userName)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/student",
            new
            {
                Email = email,
                Username = userName,
                Password
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await ConfirmEmailAsync(await GetVerificationTokenAsync(email));
        var accessToken = (await LoginAsync(email, Password)).Login.AccessToken;
        return new TestStudent(accessToken);
    }

    private async Task<TestAddedStudent> AddStudentAsync(
        string accessToken,
        decimal? hourlyRate = null)
    {
        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/tutors/me/students",
                accessToken,
                new
                {
                    DisplayName = "Managed Student",
                    Title = "Test Agreement",
                    Subject = "Mathematics",
                    HourlyRate = hourlyRate
                }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content
            .ReadFromJsonAsync<AddStudentManuallyResponse>();
        Assert.NotNull(result);

        return new TestAddedStudent(result!.StudentId, result.AgreementId);
    }

    private async Task<HttpResponseMessage> UpdateHourlyRateAsync(
        string accessToken,
        Guid studentId,
        decimal? hourlyRate)
    {
        return await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Put,
                $"{Endpoint}/{studentId}/hourly-rate",
                accessToken,
                new { HourlyRate = hourlyRate }));
    }

    private Task<TutoringAgreement> GetAgreementAsync(Guid agreementId)
    {
        return ExecuteDbAsync(dbContext =>
            dbContext.TutoringAgreements
                .AsNoTracking()
                .SingleAsync(item =>
                    item.Id == new TutoringAgreementId(agreementId)));
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

    private sealed record TestTutor(string AccessToken);

    private sealed record TestStudent(string AccessToken);

    private sealed record TestAddedStudent(Guid StudentId, Guid AgreementId);
}
