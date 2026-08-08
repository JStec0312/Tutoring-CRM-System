namespace Tutoring.Api.Students.CreateStudent;

public sealed record CreateStudentRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber);