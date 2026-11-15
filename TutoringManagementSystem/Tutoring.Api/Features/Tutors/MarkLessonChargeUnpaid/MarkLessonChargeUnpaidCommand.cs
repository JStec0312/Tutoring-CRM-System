using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.MarkLessonChargeUnpaid;

public sealed record MarkLessonChargeUnpaidCommand(
    UserAccountId AccountId, LessonChargeId ChargeId, RequestMetadata RequestMetadata) : IRequest;
