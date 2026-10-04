namespace Tutoring.Api.Features.Tutors.GetTutoringAgreementBalance;

public sealed record GetTutoringAgreementBalanceResponse(
    decimal TotalCharged,
    decimal TotalPaid,
    decimal Balance);
