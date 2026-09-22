using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Lessons;

namespace Tutoring.Api.Features.Lessons.CancelLesson;

public sealed record CancelLessonCommand(
    UserAccountId UserAccountId,
    Guid LessonId,
    LessonCancellationParty? CancellationParty,
    string? Reason,
    RequestMetadata RequestMetadata)
    : IRequest;