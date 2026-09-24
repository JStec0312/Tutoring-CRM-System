using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.GetAssignedStudents;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.UpdateStudentDetails;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdateStudentDetailsTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";

    [Fact]
    public async Task UpdateStudentDetails_ShouldPersistAllFieldsAndBeVisibleInAssignedStudents()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var studentId = await AddStudentAsync(tutor.AccessToken);

        var response = await UpdateAsync(
            tutor.AccessToken,
            studentId,
            new
            {
                Subject = "Mathematics",
                HourlyRate = 80m,
                ContactEmail = "parent@example.com",
                ContactPhoneNumber = "500600700",
                Notes = "Preparing for the final exam."
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var students = await GetAssignedStudentsAsync(tutor.AccessToken);
        var result = Assert.Single(students);
        Assert.Equal("Mathematics", result.Subject);
        Assert.Equal(80m, result.HourlyRate);
        Assert.Equal("parent@example.com", result.ContactEmail);
        Assert.Equal("500600700", result.ContactPhoneNumber);
        Assert.Equal("Preparing for the final exam.", result.Notes);
    }

    [Fact]
    public async Task UpdateStudentDetails_ShouldAllowUpdatingAndClearingOptionalFields()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var studentId = await AddStudentAsync(tutor.AccessToken);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await UpdateAsync(
                tutor.AccessToken,
                studentId,
                new
                {
                    Subject = "Physics",
                    HourlyRate = 100m,
                    ContactEmail = "parent@example.com",
                    ContactPhoneNumber = "500600700",
                    Notes = "Initial notes"
                })).StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await UpdateAsync(
                tutor.AccessToken,
                studentId,
                new
                {
                    Subject = "Chemistry",
                    HourlyRate = (decimal?)null,
                    ContactEmail = (string?)null,
                    ContactPhoneNumber = (string?)null,
                    Notes = (string?)null
                })).StatusCode);

        var result = Assert.Single(await GetAssignedStudentsAsync(tutor.AccessToken));
        Assert.Equal("Chemistry", result.Subject);
        Assert.Null(result.HourlyRate);
        Assert.Null(result.ContactEmail);
        Assert.Null(result.ContactPhoneNumber);
        Assert.Null(result.Notes);
    }

    [Fact]
    public async Task UpdateStudentDetails_WithNegativeHourlyRate_ShouldReturnBadRequest()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var studentId = await AddStudentAsync(tutor.AccessToken);

        var response = await UpdateAsync(
            tutor.AccessToken,
            studentId,
            new
            {
                Subject = "Mathematics",
                HourlyRate = -1m,
                ContactEmail = (string?)null,
                ContactPhoneNumber = (string?)null,
                Notes = (string?)null
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStudentDetails_WhenAgreementBelongsToAnotherTutor_ShouldReturnNotFound()
    {
        var owner = await CreateTutorAsync("owner@test.pl", "owner");
        var otherTutor = await CreateTutorAsync("other@test.pl", "other");
        var studentId = await AddStudentAsync(owner.AccessToken);

        var response = await UpdateAsync(
            otherTutor.AccessToken,
            studentId,
            ValidRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStudentDetails_WhenStudentOrAgreementDoesNotExist_ShouldReturnNotFound()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");

        var response = await UpdateAsync(
            tutor.AccessToken,
            Guid.NewGuid(),
            ValidRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStudentDetails_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/tutors/students/{Guid.NewGuid()}/details")
        {
            Content = JsonContent.Create(ValidRequest())
        };

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpResponseMessage> UpdateAsync(
        string accessToken,
        Guid studentId,
        object request)
    {
        return await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Put,
                $"/api/tutors/students/{studentId}/details",
                accessToken,
                request));
    }

    private async Task<List<AssignedStudentResponse>> GetAssignedStudentsAsync(
        string accessToken)
    {
        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/tutors/me/students",
                accessToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content
            .ReadFromJsonAsync<List<AssignedStudentResponse>>())!;
    }

    private async Task<Guid> AddStudentAsync(string accessToken)
    {
        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/tutors/me/students",
                accessToken,
                new
                {
                    DisplayName = "Managed Student",
                    Title = "Tutoring",
                    Subject = "Initial subject",
                    HourlyRate = (decimal?)null
                }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content
            .ReadFromJsonAsync<AddStudentResult>();
        Assert.NotNull(result);

        return result!.StudentId;
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

    private static object ValidRequest() => new
    {
        Subject = "Mathematics",
        HourlyRate = 80m,
        ContactEmail = "parent@example.com",
        ContactPhoneNumber = "500600700",
        Notes = "Notes"
    };

    private sealed record AddStudentResult(Guid StudentId, Guid AgreementId);

    private sealed record TestTutor(string AccessToken);
}
