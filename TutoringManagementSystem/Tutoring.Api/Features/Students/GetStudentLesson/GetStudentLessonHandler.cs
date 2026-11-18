using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Lessons.Exceptions;
using Tutoring.Domain.Lessons;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Students.GetStudentLesson;

public sealed class GetStudentLessonHandler(TutoringDbContext db)
    : IRequestHandler<GetStudentLessonQuery, StudentLessonDetailsResponse>
{
    public async Task<StudentLessonDetailsResponse> Handle(GetStudentLessonQuery request, CancellationToken cancellationToken)
    {
        var lessonId = new LessonId(request.LessonId);
        var lesson = await db.Lessons.AsNoTracking()
            .Include(lesson => lesson.Agreement).ThenInclude(agreement => agreement.Tutor).ThenInclude(tutor => tutor.Account)
            .SingleOrDefaultAsync(lesson => lesson.Id == lessonId &&
                lesson.Agreement.Student.UserAccountId == request.AccountId, cancellationToken);
        if (lesson is null)
        {
            throw new LessonNotFoundForUserException(request.LessonId);
        }

        var agreement = lesson.Agreement;
        var rate = agreement.HourlyRate;
        var estimate = rate?.CalculateCost(lesson.TimeSlot.EndsAtUtc - lesson.TimeSlot.StartsAtUtc);
        return new StudentLessonDetailsResponse(lesson.Id.Value, agreement.Id.Value,
            agreement.Subject.Name, lesson.TimeSlot.StartsAtUtc, lesson.TimeSlot.EndsAtUtc,
            lesson.Status.ToString(), lesson.PlannedTopic, lesson.CancellationReason?.Text,
            agreement.Tutor.Account.Profile.FirstName, agreement.Tutor.Account.Profile.LastName,
            estimate?.Amount, estimate?.Currency.Code);
    }
}
