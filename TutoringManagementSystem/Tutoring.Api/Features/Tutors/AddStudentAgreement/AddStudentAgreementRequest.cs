using System.ComponentModel.DataAnnotations;

namespace Tutoring.Api.Features.Tutors.AddStudentAgreement;

public sealed record AddStudentAgreementRequest(
    [Required, StringLength(100)] string Title,
    [Required, StringLength(100)] string Subject,
    decimal? HourlyRate);
