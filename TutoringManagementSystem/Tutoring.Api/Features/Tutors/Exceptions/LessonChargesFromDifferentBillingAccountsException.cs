using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Tutors.Exceptions;

public sealed class LessonChargesFromDifferentBillingAccountsException : ConflictException
{
    public LessonChargesFromDifferentBillingAccountsException() : base("Billing.LessonChargesFromDifferentBillingAccounts", "Lesson charges belong to different billing accounts.")
    {
    }
}