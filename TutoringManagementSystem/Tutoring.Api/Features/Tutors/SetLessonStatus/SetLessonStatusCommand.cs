using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Lessons;

namespace Tutoring.Api.Features.Tutors.SetLessonStatus;

public sealed record SetLessonStatusCommand(
    UserAccountId UserAccountId,
    Guid LessonId,
    LessonStatus Status,
    RequestMetadata RequestMetadata)
    : IRequest;