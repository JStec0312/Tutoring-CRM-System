using MediatR;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Students.GetStudentLessons;

public sealed record GetStudentLessonsQuery(UserAccountId AccountId, Guid AgreementId) : IRequest<IReadOnlyList<StudentLessonResponse>>;
