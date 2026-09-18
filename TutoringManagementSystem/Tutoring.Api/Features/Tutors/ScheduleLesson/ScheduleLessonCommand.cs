using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.ScheduleLesson;

public sealed record ScheduleLessonCommand(
    UserAccountId UserAccountId,
    Guid TutoringAgreementId,
    DateTimeOffset StartsAt,
    int DurationMinutes,
    RequestMetadata RequestMetadata)
    : IRequest<ScheduleLessonResponse>;
