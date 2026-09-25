using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetStudentLessonHistory;

public sealed record GetStudentLessonHistoryQuery(
    UserAccountId UserAccountId,
    Guid StudentId,
    RequestMetadata RequestMetadata)
    : IRequest<IReadOnlyCollection<StudentLessonHistoryResponse>>;
