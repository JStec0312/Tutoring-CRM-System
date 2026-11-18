using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Students.Exceptions;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Students.GetStudentLessons;

public sealed class GetStudentLessonsHandler(TutoringDbContext db)
    : IRequestHandler<GetStudentLessonsQuery, IReadOnlyList<StudentLessonResponse>>
{
    public async Task<IReadOnlyList<StudentLessonResponse>> Handle(
        GetStudentLessonsQuery request, CancellationToken cancellationToken)
    {
        var agreementId = new TutoringAgreementId(request.AgreementId);
        if (!await db.TutoringAgreements.AsNoTracking().AnyAsync(agreement =>
            agreement.Id == agreementId && agreement.Student.UserAccountId == request.AccountId, cancellationToken))
        {
            throw new StudentAgreementNotFoundException();
        }

        return await db.Lessons.AsNoTracking()
            .Where(lesson => lesson.TutoringAgreementId == agreementId)
            .OrderByDescending(lesson => lesson.TimeSlot.StartsAtUtc).ThenBy(lesson => lesson.Id)
            .Select(lesson => new StudentLessonResponse(lesson.Id.Value, lesson.TutoringAgreementId.Value,
                lesson.Agreement.Subject.Name, lesson.TimeSlot.StartsAtUtc, lesson.TimeSlot.EndsAtUtc,
                lesson.Status.ToString(), lesson.PlannedTopic,
                lesson.CancellationReason == null ? null : lesson.CancellationReason.Text))
            .ToListAsync(cancellationToken);
    }
}
