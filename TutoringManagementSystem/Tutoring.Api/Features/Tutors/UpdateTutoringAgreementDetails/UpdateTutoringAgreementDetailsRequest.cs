namespace Tutoring.Api.Features.Tutors.UpdateTutoringAgreementDetails;

public sealed record UpdateTutoringAgreementDetailsRequest(
    string? Subject,
    string? Notes);
