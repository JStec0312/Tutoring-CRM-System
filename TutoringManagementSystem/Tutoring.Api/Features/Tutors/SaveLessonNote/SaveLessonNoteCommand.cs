using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.SaveLessonNote;

public sealed record SaveLessonNoteCommand(UserAccountId UserAccountId, Guid LessonId,
    string? Topic, string? CoveredTopics, string? StudentProgress, RequestMetadata RequestMetadata) : IRequest;
