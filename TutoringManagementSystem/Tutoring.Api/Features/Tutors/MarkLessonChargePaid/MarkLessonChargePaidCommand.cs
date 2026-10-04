using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.MarkLessonChargePaid;

public sealed record MarkLessonChargePaidCommand(
    UserAccountId UserAccountId,
    Guid LessonChargeId,
    DateTimeOffset? PaidAtUtc,
    string? Reference,
    RequestMetadata RequestMetadata) : IRequest;
