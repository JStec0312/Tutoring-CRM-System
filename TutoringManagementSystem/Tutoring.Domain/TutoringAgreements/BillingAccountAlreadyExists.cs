using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.TutoringAgreements;

public sealed class BillingAccountAlreadyExistsException : DomainException
{
    public BillingAccountAlreadyExistsException()
        : base("TutoringAgreement.BillingAccountAlreadyExists", "Billing account already exists.")
    {
    }
}
