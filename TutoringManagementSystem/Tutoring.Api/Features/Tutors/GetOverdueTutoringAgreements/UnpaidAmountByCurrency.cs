namespace Tutoring.Api.Features.Tutors.GetOverdueTutoringAgreements;

public sealed record UnpaidAmountByCurrency(
    string Currency,
    decimal Amount,
    int UnpaidLessonCount);
