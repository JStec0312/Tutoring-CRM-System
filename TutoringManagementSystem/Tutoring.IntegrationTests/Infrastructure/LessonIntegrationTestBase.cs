using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Billing;
using Tutoring.Domain.LessonSeries;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.IntegrationTests.Infrastructure;

public abstract class LessonIntegrationTestBase(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    protected abstract DateTimeOffset AgreementCreatedAtUtc { get; }

    protected async Task<TestTutor> CreateTutorAsync(string email, string userName)
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

        return new TestTutor(email, accessToken);
    }

    protected async Task<TestStudent> CreateStudentAsync(string email, string userName)
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
        var studentId = await ExecuteDbAsync(dbContext =>
            dbContext.Students
                .Where(student => student.Account != null && student.Account.Email.Value == email)
                .Select(student => student.Id.Value)
                .SingleAsync());

        return new TestStudent(accessToken, studentId);
    }

    protected async Task<Guid> CreateAgreementAsync(string tutorEmail, Guid studentId)
    {
        return await ExecuteDbAsync(async dbContext =>
        {
            var tutorId = await dbContext.Tutors
                .Where(tutor => tutor.Account.Email.Value == tutorEmail)
                .Select(tutor => tutor.Id)
                .SingleAsync();

            var agreement = new TutoringAgreement(
                tutorId,
                new StudentId(studentId),
                new Subject("Mathematics"),
                new HourlyRate(new Money(100, new Currency("PLN"))),
                new AgreementTitle("Test Agreement"),
                AgreementCreatedAtUtc);

            dbContext.TutoringAgreements.Add(agreement);
            await dbContext.SaveChangesAsync();

            return agreement.Id.Value;
        });
    }

    protected async Task<Guid> CreateLessonAsync(
        Guid agreementId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        LessonSeriesId? lessonSeriesId = null)
    {
        return await ExecuteDbAsync(async dbContext =>
        {
            var lesson = new Lesson(
                new TutoringAgreementId(agreementId),
                new TimeSlot(startsAtUtc, endsAtUtc),
                AgreementCreatedAtUtc,
                lessonSeriesId);
            dbContext.Lessons.Add(lesson);
            await dbContext.SaveChangesAsync();
            return lesson.Id.Value;
        });
    }

    protected sealed record TestTutor(string Email, string AccessToken);

    protected sealed record TestStudent(string AccessToken, Guid StudentId);
}
