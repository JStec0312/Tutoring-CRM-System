using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;
namespace Tutoring.Api.Features.Tutors.GetAssignedStudents;

public sealed class GetAssignedStudentsHandler(
    TutoringDbContext dbContext)
    : IRequestHandler<
        GetAssignedStudentsQuery,
        IReadOnlyCollection<AssignedStudentResponse>>
{
    public async Task<IReadOnlyCollection<AssignedStudentResponse>> Handle(
        GetAssignedStudentsQuery request,
        CancellationToken cancellationToken)
    {
        var tutor = await dbContext.Tutors
            .AsNoTracking()
            .SingleOrDefaultAsync(
                tutor => tutor.UserAccountId == request.UserAccountId,
                cancellationToken);

        if (tutor is null)
        {
            return [];
        }

        var students = await dbContext.TutoringAgreements
            .AsQueryable()
            .Where(agreement =>
                agreement.TutorId == tutor.Id &&
                agreement.Status != AgreementStatus.Ended)
            .Select(agreement => new AssignedStudentResponse(
                agreement.Student.Id.Value,
                agreement.Student.Account.Profile.UserName,
                agreement.Student.Account.Profile.FirstName,
                agreement.Student.Account.Profile.LastName,
                agreement.Student.Account.Email.Value,
                agreement.Student.Account.Profile.PhoneNumber != null
                    ? agreement.Student.Account.Profile.PhoneNumber.Value
                    : null,
                agreement.Student.Status.ToString()))
            .ToListAsync(cancellationToken);
        return students;
    }
}