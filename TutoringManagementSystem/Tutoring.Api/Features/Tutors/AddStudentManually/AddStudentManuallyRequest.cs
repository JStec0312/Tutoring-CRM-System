namespace Tutoring.Api.Features.Tutors.AddStudentManually;

public sealed record AddStudentManuallyRequest(
    string DisplayName,
    string Title,
    string Subject,
    decimal? HourlyRate);