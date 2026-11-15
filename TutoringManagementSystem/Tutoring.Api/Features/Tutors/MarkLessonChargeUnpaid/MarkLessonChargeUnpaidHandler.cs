using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Common.Authorization;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Billing;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.MarkLessonChargeUnpaid;

public sealed class MarkLessonChargeUnpaidHandler(TutoringDbContext db, ILogger<MarkLessonChargeUnpaidHandler> logger)
    : IRequestHandler<MarkLessonChargeUnpaidCommand>
{
    public async Task Handle(MarkLessonChargeUnpaidCommand request, CancellationToken cancellationToken)
    {
        var ownAgreements = db.TutoringAgreements.ForTutor(request.AccountId);

        // Keep the payment and its sibling charge allocations stable until the correction commits.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        try
        {
            var account = await db.BillingAccounts
                .Include(account => account.Charges)
                .Include(account => account.Payments)
                .AsSplitQuery()
                .SingleOrDefaultAsync(account => account.Charges.Any(charge => charge.Id == request.ChargeId) &&
                    ownAgreements.Any(agreement => agreement.Id == account.TutoringAgreementId), cancellationToken)
                ?? throw new LessonChargeNotFoundException(request.AccountId.Value, request.ChargeId.Value);

            var removedPayment = account.MarkChargeAsUnpaid(request.ChargeId);
            if (removedPayment is not null)
                db.Payments.Remove(removedPayment);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateConcurrencyException or SqlException { Number: 1205 } ||
            exception is DbUpdateException { InnerException: SqlException { Number: 1205 } })
        {
            throw new LessonChargePaymentChangedException();
        }

        logger.LogInformation("Lesson charge marked as unpaid. AccountId: {AccountId}, ChargeId: {ChargeId}, RequestMetadata: {RequestMetadata}",
            request.AccountId.Value, request.ChargeId.Value, request.RequestMetadata);
    }
}
