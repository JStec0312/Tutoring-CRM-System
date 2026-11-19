using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Lessons.GetMyLessons;

public sealed record GetMyLessonsQuery(UserAccountId AccountId, bool TutorOnly, int Page, int PageSize,
    string View, Guid? AgreementId, RequestMetadata RequestMetadata) : IRequest<LessonPageResponse>;
