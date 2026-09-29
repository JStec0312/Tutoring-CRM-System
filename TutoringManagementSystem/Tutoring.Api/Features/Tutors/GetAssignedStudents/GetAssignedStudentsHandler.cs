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

        var agreements = await dbContext.TutoringAgreements
            .AsNoTracking()
            .Where(agreement =>
                agreement.TutorId == tutor.Id &&
                agreement.Status != AgreementStatus.Ended)
            .Select(agreement => new
            {
                StudentId = agreement.Student.Id.Value,
                DisplayName = agreement.Student.DisplayName.Value,

                FirstName = agreement.Student.Account != null
                    ? agreement.Student.Account.Profile.FirstName
                    : null,

                LastName = agreement.Student.Account != null
                    ? agreement.Student.Account.Profile.LastName
                    : null,

                Email = agreement.Student.Account != null
                    ? agreement.Student.Account.Email.Value
                    : null,

                PhoneNumber =
                    agreement.Student.Account != null &&
                    agreement.Student.Account.Profile.PhoneNumber != null
                        ? agreement.Student.Account.Profile.PhoneNumber.Value
                        : null,

                StudentStatus = agreement.Student.Status.ToString(),

                TutoringAgreementId = agreement.Id.Value,

                Subject = agreement.Subject.Name,

                HourlyRate = agreement.HourlyRate != null
                    ? (decimal?)agreement.HourlyRate.PricePerHour.Amount
                    : null,

                ContactEmail = agreement.ContactEmail != null
                    ? agreement.ContactEmail.Value
                    : null,

                ContactPhoneNumber = agreement.ContactPhoneNumber != null
                    ? agreement.ContactPhoneNumber.Value
                    : null,

                Notes = agreement.PrivateNotes
            })
            .ToListAsync(cancellationToken);

        var students = agreements
            .GroupBy(agreement => new
            {
                agreement.StudentId,
                agreement.DisplayName,
                agreement.FirstName,
                agreement.LastName,
                agreement.Email,
                agreement.PhoneNumber,
                agreement.StudentStatus
            })
            .Select(group => new AssignedStudentResponse(
                group.Key.StudentId,
                group.Key.DisplayName,
                group.Key.FirstName,
                group.Key.LastName,
                group.Key.Email,
                group.Key.PhoneNumber,
                group.Key.StudentStatus,
                group.Select(agreement =>
                        new StudentAgreementResponse(
                            agreement.TutoringAgreementId,
                            agreement.Subject,
                            agreement.HourlyRate,
                            agreement.ContactEmail,
                            agreement.ContactPhoneNumber,
                            agreement.Notes))
                    .ToList()))
            .ToList();

        return students;
    }
}