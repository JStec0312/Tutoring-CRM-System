using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.GetAssignedStudents;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Domain.Tutors;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.Tutors.GetAssignedStudents;

public sealed class GetAssignedStudentsTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string Password = "Password123!";
    private const string Endpoint = "/api/tutors/me/students";

    [Fact]
    public async Task GetAssignedStudents_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var response = await Client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAssignedStudents_AsStudent_ShouldReturnForbidden()
    {
        var student = await CreateStudentAsync(
            "student@test.pl",
            "student",
            "Student",
            "User",
            null);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Get,
                Endpoint,
                student.AccessToken));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAssignedStudents_WithoutAssignments_ShouldReturnEmptyList()
    {
        var tutor = await CreateTutorAsync(
            "tutor@test.pl",
            "tutor",
            "Tutor",
            "One",
            "+48111111111");

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Get,
                Endpoint,
                tutor.AccessToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var students = await response.Content
            .ReadFromJsonAsync<List<AssignedStudentResponse>>();

        Assert.NotNull(students);
        Assert.Empty(students!);
    }

    [Fact]
    public async Task GetAssignedStudents_ShouldMapAssignedStudentData()
    {
        var tutor = await CreateTutorAsync(
            "tutor@test.pl",
            "tutor",
            "Tutor",
            "One",
            "+48111111111");
        var student = await CreateStudentAsync(
            "student@test.pl",
            "student",
            "First",
            "Student",
            "+48222222222");

        await CreateAgreementAsync(tutor.Email, student.Email);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Get,
                Endpoint,
                tutor.AccessToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var students = await response.Content
            .ReadFromJsonAsync<List<AssignedStudentResponse>>();

        var result = Assert.Single(students!);
        Assert.Equal(student.StudentId, result.StudentId);
        Assert.Equal("student", result.DisplayName);
        Assert.Equal("First", result.FirstName);
        Assert.Equal("Student", result.LastName);
        Assert.Equal(student.Email, result.Email);
        Assert.Equal("+48222222222", result.PhoneNumber);
        Assert.Equal(StudentStatus.Active.ToString(), result.Status);
    }

    [Fact]
    public async Task GetAssignedStudents_ForStudentWithoutAccount_ShouldReturnNullPersonalFields()
    {
        var tutor = await CreateTutorAsync(
            "tutor@test.pl",
            "tutor",
            "Tutor",
            "One",
            "+48111111111");
        var managedStudentId = await CreateManagedStudentAsync("Managed Student");

        await CreateAgreementAsync(tutor.Email, managedStudentId);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Get,
                Endpoint,
                tutor.AccessToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var students = await response.Content
            .ReadFromJsonAsync<List<AssignedStudentResponse>>();

        var result = Assert.Single(students!);
        Assert.Equal(managedStudentId, result.StudentId);
        Assert.Equal("Managed Student", result.DisplayName);
        Assert.Null(result.FirstName);
        Assert.Null(result.LastName);
        Assert.Null(result.Email);
        Assert.Null(result.PhoneNumber);
        Assert.Equal(StudentStatus.Active.ToString(), result.Status);
    }

    [Fact]
    public async Task GetAssignedStudents_WithRegisteredAndManagedStudents_ShouldReturnBoth()
    {
        var tutor = await CreateTutorAsync(
            "tutor@test.pl",
            "tutor",
            "Tutor",
            "One",
            "+48111111111");
        var registeredStudent = await CreateStudentAsync(
            "student@test.pl",
            "student",
            "Registered",
            "Student",
            "+48222222222");
        var managedStudentId = await CreateManagedStudentAsync("Managed Student");

        await CreateAgreementAsync(tutor.Email, registeredStudent.Email);
        await CreateAgreementAsync(tutor.Email, managedStudentId);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Get,
                Endpoint,
                tutor.AccessToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var students = await response.Content
            .ReadFromJsonAsync<List<AssignedStudentResponse>>();

        Assert.NotNull(students);
        Assert.Equal(2, students!.Count);

        var registered = students.Single(item =>
            item.StudentId == registeredStudent.StudentId);
        Assert.Equal("student", registered.DisplayName);
        Assert.Equal("Registered", registered.FirstName);
        Assert.Equal("Student", registered.LastName);
        Assert.Equal(registeredStudent.Email, registered.Email);
        Assert.Equal("+48222222222", registered.PhoneNumber);

        var managed = students.Single(item =>
            item.StudentId == managedStudentId);
        Assert.Equal("Managed Student", managed.DisplayName);
        Assert.Null(managed.FirstName);
        Assert.Null(managed.LastName);
        Assert.Null(managed.Email);
        Assert.Null(managed.PhoneNumber);
    }

    [Fact]
    public async Task GetAssignedStudents_ShouldReturnOnlyStudentsAssignedToAuthenticatedTutor()
    {
        var tutorA = await CreateTutorAsync(
            "tutor-a@test.pl",
            "tutor-a",
            "Tutor",
            "A",
            "+48111111111");
        var student1 = await CreateStudentAsync(
            "student-1@test.pl",
            "student-1",
            "Student",
            "One",
            null);
        var student2 = await CreateStudentAsync(
            "student-2@test.pl",
            "student-2",
            "Student",
            "Two",
            null);
        var tutorB = await CreateTutorAsync(
            "tutor-b@test.pl",
            "tutor-b",
            "Tutor",
            "B",
            "+48333333333");
        var student3 = await CreateStudentAsync(
            "student-3@test.pl",
            "student-3",
            "Student",
            "Three",
            null);

        await CreateAgreementAsync(tutorA.Email, student1.Email);
        await CreateAgreementAsync(tutorA.Email, student2.Email);
        await CreateAgreementAsync(tutorB.Email, student3.Email);

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Get,
                Endpoint,
                tutorA.AccessToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var students = await response.Content
            .ReadFromJsonAsync<List<AssignedStudentResponse>>();

        Assert.NotNull(students);
        Assert.Equal(
            new[] { student1.StudentId, student2.StudentId }.OrderBy(id => id),
            students!.Select(student => student.StudentId).OrderBy(id => id));
    }

    [Fact]
    public async Task GetAssignedStudents_ShouldExcludeEndedAgreements()
    {
        var tutor = await CreateTutorAsync(
            "tutor@test.pl",
            "tutor",
            "Tutor",
            "One",
            null);
        var activeStudent = await CreateStudentAsync(
            "active@test.pl",
            "active",
            "Active",
            "Student",
            null);
        var endedStudent = await CreateStudentAsync(
            "ended@test.pl",
            "ended",
            "Ended",
            "Student",
            null);

        await CreateAgreementAsync(tutor.Email, activeStudent.Email);
        var endedAgreement = await CreateAgreementAsync(
            tutor.Email,
            endedStudent.Email);

        await ExecuteDbAsync(async dbContext =>
        {
            var agreement = await dbContext.TutoringAgreements
                .SingleAsync(item => item.Id == endedAgreement.Id);
            dbContext.Entry(agreement).Property(item => item.Status)
                .CurrentValue = AgreementStatus.Ended;
            await dbContext.SaveChangesAsync();
        });

        var response = await Client.SendAsync(
            CreateAuthorizedRequest(
                HttpMethod.Get,
                Endpoint,
                tutor.AccessToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var students = await response.Content
            .ReadFromJsonAsync<List<AssignedStudentResponse>>();

        var result = Assert.Single(students!);
        Assert.Equal(activeStudent.StudentId, result.StudentId);
    }

    private async Task<TestTutor> CreateTutorAsync(
        string email,
        string userName,
        string firstName,
        string lastName,
        string? phoneNumber)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register/tutor",
            new
            {
                Email = email,
                UserName = userName,
                Password,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await ConfirmEmailAsync(await GetVerificationTokenAsync(email));
        var accessToken = (await LoginAsync(email, Password)).Login.AccessToken;
        var tutorId = await ExecuteDbAsync(dbContext =>
            dbContext.Tutors
                .Where(tutor => tutor.Account.Email.Value == email)
                .Select(tutor => tutor.Id.Value)
                .SingleAsync());

        return new TestTutor(email, accessToken, tutorId);
    }

    private async Task<TestStudent> CreateStudentAsync(
        string email,
        string userName,
        string firstName,
        string lastName,
        string? phoneNumber)
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

        using var request = CreateAuthorizedRequest(
            HttpMethod.Put,
            "/api/users/me/profile",
            accessToken,
            new
            {
                UserName = userName,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber
            });
        var profileResponse = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, profileResponse.StatusCode);

        var studentId = await ExecuteDbAsync(dbContext =>
            dbContext.Students
                .Where(student =>
                    student.Account != null &&
                    student.Account.Email.Value == email)
                .Select(student => student.Id.Value)
                .SingleAsync());

        return new TestStudent(email, accessToken, studentId);
    }

    /// <summary>
    /// Creates a managed student that has no linked UserAccount
    /// (e.g. added by a tutor without inviting/registering the student).
    /// </summary>
    private async Task<Guid> CreateManagedStudentAsync(
        string displayName)
    {
        return await ExecuteDbAsync(async dbContext =>
        {
            var student = new Student(
                new StudentDisplayName(displayName),
                DateTimeOffset.UtcNow);

            dbContext.Students.Add(student);
            await dbContext.SaveChangesAsync();

            return student.Id.Value;
        });
    }

    private async Task<TutoringAgreement> CreateAgreementAsync(
        string tutorEmail,
        string studentEmail)
    {
        var studentId = await ExecuteDbAsync(dbContext =>
            dbContext.Students
                .Where(student =>
                    student.Account != null &&
                    student.Account.Email.Value == studentEmail)
                .Select(student => student.Id.Value)
                .SingleAsync());

        return await CreateAgreementAsync(tutorEmail, studentId);
    }

    private async Task<TutoringAgreement> CreateAgreementAsync(
        string tutorEmail,
        Guid studentId)
    {
        return await ExecuteDbAsync(async dbContext =>
        {
            var tutorId = await dbContext.Tutors
                .Where(tutor => tutor.Account.Email.Value == tutorEmail)
                .Select(tutor => tutor.Id)
                .SingleAsync();
            string testTitle = "Test Agreement Title";
            var agreement = new TutoringAgreement(
                tutorId,
                new StudentId(studentId),
                new Subject("Mathematics"),
                new HourlyRate(new Money(100, new Currency("PLN"))),
                new AgreementTitle(testTitle),

                DateTimeOffset.UtcNow);

            dbContext.TutoringAgreements.Add(agreement);
            await dbContext.SaveChangesAsync();
            return agreement;
        });
    }

    private sealed record TestTutor(
        string Email,
        string AccessToken,
        Guid TutorId);

    private sealed record TestStudent(
        string Email,
        string AccessToken,
        Guid StudentId);
}
