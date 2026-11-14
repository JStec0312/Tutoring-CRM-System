using MediatR;
using Tutoring.Api.Features.Calendar.GetCalendarLessons;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Lessons;

namespace Tutoring.Api.Features.Tutors.GetTutorLesson;

public sealed record GetTutorLessonQuery(UserAccountId AccountId, LessonId LessonId)
    : IRequest<TutorLessonResponse>;
