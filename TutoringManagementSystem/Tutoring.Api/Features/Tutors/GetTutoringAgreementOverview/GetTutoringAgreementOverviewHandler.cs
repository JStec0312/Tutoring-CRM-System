using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Calendar.GetCalendarLessons;
using Tutoring.Api.Features.Common.Authorization;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Domain.Lessons;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.GetTutoringAgreementOverview;

public sealed class GetTutoringAgreementOverviewHandler(TutoringDbContext db, TimeProvider clock)
    : IRequestHandler<GetTutoringAgreementOverviewQuery, TutoringAgreementOverviewResponse>
{
    public async Task<TutoringAgreementOverviewResponse> Handle(
        GetTutoringAgreementOverviewQuery request, CancellationToken cancellationToken)
    {
        var agreement = await db.TutoringAgreements.AsNoTracking()
            .ForTutor(request.AccountId)
            .Where(agreement => agreement.Id == request.AgreementId)
            .Select(agreement => new
            {
                agreement.StudentId,
                StudentName = agreement.Student.DisplayName.Value,
                Title = agreement.AgreementTitle.Value,
                Subject = agreement.Subject.Name,
                agreement.Status,
                agreement.HourlyRate,
                agreement.PrivateNotes,
                agreement.TutorId
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new TutoringAgreementNotFoundByIdException(request.AccountId.Value, request.AgreementId.Value);

        var now = clock.GetUtcNow();
        var next = await db.Lessons.AsNoTracking()
            .Where(lesson => lesson.TutoringAgreementId == request.AgreementId
                && lesson.Status == LessonStatus.Scheduled
                && lesson.TimeSlot.EndsAtUtc > now)
            .OrderBy(lesson => lesson.TimeSlot.StartsAtUtc)
            .Select(lesson => new { lesson.Id, lesson.LessonSeriesId, lesson.TimeSlot })
            .FirstOrDefaultAsync(cancellationToken);

        var materialCount = await db.LearningMaterials.AsNoTracking()
            .CountAsync(material => material.TutoringAgreementId == request.AgreementId
                && (material.Status == MaterialStatus.Published || material.Status == MaterialStatus.Draft),
                cancellationToken);

        var nextLesson = next is null ? null : GetCalendarLessonsHandler.WithEstimate(new CalendarLessonResponse(
            next.Id.Value, request.AgreementId.Value, next.LessonSeriesId?.Value,
            agreement.Title, agreement.Subject, next.TimeSlot.StartsAtUtc, next.TimeSlot.EndsAtUtc,
            nameof(LessonStatus.Scheduled), agreement.StudentId.Value, agreement.StudentName,
            agreement.TutorId.Value, null, null), agreement.HourlyRate);

        return new TutoringAgreementOverviewResponse(
            agreement.StudentId.Value, agreement.StudentName, agreement.Subject, agreement.Status.ToString(),
            agreement.HourlyRate?.PricePerHour.Amount, agreement.HourlyRate?.PricePerHour.Currency.Code,
            agreement.PrivateNotes, nextLesson, materialCount);
    }
}
