using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.UpdateTutoringAgreementDetails;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdateTutoringAgreementDetailsTests(
    IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/tutoring-agreements";

    [Fact]
    public async Task UpdateDetails_WithSubject_ShouldPersistSubject()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var id = await CreateAgreementAsync(tutor.Email);

        var response = await UpdateAsync(id, tutor.AccessToken, new { Subject = "Physics", Notes = (string?)null });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertDetailsAsync(id, "Physics", null);
    }

    [Fact]
    public async Task UpdateDetails_WithNotes_ShouldPersistNotes()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var id = await CreateAgreementAsync(tutor.Email);

        var response = await UpdateAsync(id, tutor.AccessToken, new { Subject = (string?)null, Notes = "Bring exercises." });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertDetailsAsync(id, "Mathematics", "Bring exercises.");
    }

    [Fact]
    public async Task UpdateDetails_WithSubjectAndNotes_ShouldPersistBoth()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var id = await CreateAgreementAsync(tutor.Email);

        var response = await UpdateAsync(id, tutor.AccessToken, new { Subject = "Chemistry", Notes = "Online." });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertDetailsAsync(id, "Chemistry", "Online.");
    }

    [Fact]
    public async Task UpdateDetails_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await Client.PutAsJsonAsync($"{Endpoint}/{Guid.NewGuid()}/details",
            new { Subject = "Physics", Notes = "Notes" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDetails_AsStudent_ShouldReturnForbidden()
    {
        var student = await CreateStudentAsync("student@test.pl", "student");
        var response = await UpdateAsync(Guid.NewGuid(), student.AccessToken,
            new { Subject = "Physics", Notes = "Notes" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDetails_WhenAgreementDoesNotExist_ShouldReturnNotFound()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var response = await UpdateAsync(Guid.NewGuid(), tutor.AccessToken,
            new { Subject = "Physics", Notes = "Notes" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDetails_WhenAgreementBelongsToAnotherTutor_ShouldReturnNotFoundAndNotChangeDatabase()
    {
        var owner = await CreateTutorAsync("owner@test.pl", "owner");
        var otherTutor = await CreateTutorAsync("other@test.pl", "other");
        var id = await CreateAgreementAsync(owner.Email);

        var response = await UpdateAsync(id, otherTutor.AccessToken,
            new { Subject = "Physics", Notes = "Changed" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertDetailsAsync(id, "Mathematics", null);
    }

    [Fact]
    public async Task UpdateDetails_WhenAgreementEnded_ShouldReturnNotFoundAndNotChangeDatabase()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var id = await CreateAgreementAsync(tutor.Email);
        await EndAgreementAsync(id);

        var response = await UpdateAsync(id, tutor.AccessToken,
            new { Subject = "Physics", Notes = "Changed" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertDetailsAsync(id, "Mathematics", null);
    }

    [Fact]
    public async Task UpdateDetails_WithInvalidSubject_ShouldReturnInvalidProblemDetailsAndNotChangeDatabase()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var id = await CreateAgreementAsync(tutor.Email);

        var response = await UpdateAsync(id, tutor.AccessToken,
            new { Subject = " ", Notes = "Changed" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Request.Invalid", Assert.IsType<JsonElement>(problem!.Extensions["code"]).GetString());
        await AssertDetailsAsync(id, "Mathematics", null);
    }

    private async Task<HttpResponseMessage> UpdateAsync(Guid id, string token, object body)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Put, $"{Endpoint}/{id}/details", token, body);
        return await Client.SendAsync(request);
    }

    private async Task AssertDetailsAsync(Guid id, string subject, string? notes)
    {
        var details = await ExecuteDbAsync(db => db.TutoringAgreements.AsNoTracking()
            .Where(agreement => agreement.Id == new TutoringAgreementId(id))
            .Select(agreement => new { Subject = agreement.Subject.Name, Notes = agreement.PrivateNotes })
            .SingleAsync());
        Assert.Equal(subject, details.Subject);
        Assert.Equal(notes, details.Notes);
    }

    private async Task<Guid> CreateAgreementAsync(string tutorEmail)
    {
        var studentId = await CreateStudentAsync($"student-{Guid.NewGuid()}@test.pl", Guid.NewGuid().ToString("N"));
        return await ExecuteDbAsync(async db =>
        {
            var tutorId = await db.Tutors.Where(tutor => tutor.Account.Email.Value == tutorEmail)
                .Select(tutor => tutor.Id).SingleAsync();
            var agreement = new TutoringAgreement(tutorId, new StudentId(studentId.StudentId),
                new Subject("Mathematics"), new HourlyRate(new Money(100, new Currency("PLN"))),
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

    private async Task<TestTutor> CreateTutorAsync(string email, string userName)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register/tutor",
            new { Email = email, UserName = userName, Password, FirstName = "Tutor", LastName = "Test", PhoneNumber = (string?)null });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await ConfirmEmailAsync(await GetVerificationTokenAsync(email));
        var session = await LoginAsync(email, Password);
        return new TestTutor(email, session.Login.AccessToken);
    }

    private async Task<TestStudent> CreateStudentAsync(string email, string userName)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register/student",
            new { Email = email, Username = userName, Password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await ConfirmEmailAsync(await GetVerificationTokenAsync(email));
        var session = await LoginAsync(email, Password);
        var id = await ExecuteDbAsync(db => db.Students.Where(student => student.Account!.Email.Value == email)
            .Select(student => student.Id.Value).SingleAsync());
        return new TestStudent(session.Login.AccessToken, id);
    }

    private sealed record TestTutor(string Email, string AccessToken);
    private sealed record TestStudent(string AccessToken, Guid StudentId);
}
