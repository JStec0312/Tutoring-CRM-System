namespace Tutoring.Api.Features.Tutors.InviteStudent;

public sealed record InviteStudentRequest(
    string Email,
    string Title,
    string Subject,
    decimal? HourlyRate);