using MediatR;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Students.GetStudentLesson;

public sealed record GetStudentLessonQuery(UserAccountId AccountId, Guid LessonId) : IRequest<StudentLessonDetailsResponse>;
