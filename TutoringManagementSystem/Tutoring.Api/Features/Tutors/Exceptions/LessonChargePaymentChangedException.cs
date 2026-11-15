using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Tutors.Exceptions;

public sealed class LessonChargePaymentChangedException()
    : ConflictException("Billing.PaymentChanged", "The payment changed while saving. Refresh the list and try again.");
