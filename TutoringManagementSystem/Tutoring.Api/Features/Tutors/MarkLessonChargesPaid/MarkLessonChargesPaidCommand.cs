using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.MarkLessonChargesPaid;

public sealed record MarkLessonChargesPaidCommand(
    UserAccountId UserAccountId,
    IReadOnlyCollection<Guid> LessonChargeIds,
    DateTimeOffset PaidAtUtc,
    string? Reference,
    RequestMetadata RequestMetadata) : IRequest;
