using MediatR;
using Tutoring.Api.Features.Calendar.GetCalendarLessons;
using Tutoring.Domain.Identity;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.Api.Features.Tutors.GetTutoringAgreementOverview;

public sealed record GetTutoringAgreementOverviewQuery(UserAccountId AccountId, TutoringAgreementId AgreementId)
    : IRequest<TutoringAgreementOverviewResponse>;
