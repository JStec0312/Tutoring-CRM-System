using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.RescheduleLesson;

public sealed record RescheduleLessonCommand(
    UserAccountId UserAccountId,
    Guid LessonId,
    DateTimeOffset StartsAt,
    int DurationMinutes,
    RequestMetadata RequestMetadata)
    : IRequest;