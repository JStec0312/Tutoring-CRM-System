using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Common.Authorization;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Tutors;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.AddStudentAgreement;

public sealed class AddStudentAgreementHandler(
    TutoringDbContext db, TimeProvider clock, ILogger<AddStudentAgreementHandler> logger)
    : IRequestHandler<AddStudentAgreementCommand, Guid>
{
    public async Task<Guid> Handle(AddStudentAgreementCommand request, CancellationToken cancellationToken)
    {
        var tutorId = await db.TutoringAgreements.AsNoTracking()
            .ForTutor(request.AccountId)
            .Where(agreement => agreement.StudentId == request.StudentId)
            .Select(agreement => (TutorId?)agreement.TutorId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new TutoringAgreementNotFoundException(request.AccountId.Value, request.StudentId.Value);

        var rate = request.HourlyRate is { } amount ? new HourlyRate(new Money(amount, new Currency("PLN"))) : null;
        var agreement = new TutoringAgreement(tutorId, request.StudentId, new Subject(request.Subject),
            rate, new AgreementTitle(request.Title), clock.GetUtcNow());

        db.TutoringAgreements.Add(agreement);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Tutoring agreement created. TutorId: {TutorId}, AgreementId: {AgreementId}",
            tutorId.Value, agreement.Id.Value);

        return agreement.Id.Value;
    }
}
