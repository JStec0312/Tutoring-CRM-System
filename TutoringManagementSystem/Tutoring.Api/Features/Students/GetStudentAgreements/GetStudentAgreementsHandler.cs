using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Students.GetStudentAgreements;

public sealed class GetStudentAgreementsHandler(TutoringDbContext db)
    : IRequestHandler<GetStudentAgreementsQuery, IReadOnlyList<StudentAgreementResponse>>
{
    public async Task<IReadOnlyList<StudentAgreementResponse>> Handle(
        GetStudentAgreementsQuery request, CancellationToken cancellationToken)
    {
        return await db.TutoringAgreements.AsNoTracking()
            .Where(agreement => agreement.Student.UserAccountId == request.AccountId)
            .OrderBy(agreement => agreement.Subject.Name).ThenBy(agreement => agreement.Id)
            .Select(agreement => new StudentAgreementResponse(
                agreement.Id.Value, agreement.AgreementTitle.Value, agreement.Subject.Name,
                agreement.Status.ToString(), agreement.Tutor.Account.Profile.FirstName,
                agreement.Tutor.Account.Profile.LastName,
                agreement.HourlyRate == null ? null : agreement.HourlyRate.PricePerHour.Amount,
                agreement.HourlyRate == null ? null : agreement.HourlyRate.PricePerHour.Currency.Code))
            .ToListAsync(cancellationToken);
    }
}
