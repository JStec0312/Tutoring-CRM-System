namespace Tutoring.Api.Features.Tutors.UpdateStudentDetails;

public sealed record UpdateStudentDetailsRequest(
    string Subject,
    decimal? HourlyRate,
    string? ContactEmail,
    string? ContactPhoneNumber,
    string? Notes);
