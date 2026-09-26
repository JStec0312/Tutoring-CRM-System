using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.TutoringAgreements;

public sealed class InvalidTimeToCalculateCostException : DomainException
{
    public InvalidTimeToCalculateCostException() : base("TutoringAgreement.InvalidTimeToCalculateCost", "It is not the correct time to calculate the cost for this tutoring agreement.")
    {
        
    }
}