namespace Tutoring.Api.Features.Tutors.GetTutoringAgreementBalance;

public sealed record GetTutoringAgreementBalanceResponse(
    Guid TutoringAgreementId,
    decimal TotalCharged,
    decimal TotalPaid,
    decimal Balance,
    string Currency);
