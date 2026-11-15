using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.Billing;

public sealed class ChargeCannotBeMarkedUnpaidException(Guid chargeId)
    : DomainException("Billing.ChargeCannotBeMarkedUnpaid", $"Lesson charge '{chargeId}' must be active to be marked unpaid.");
