using MediatR;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetLessonCharges;

public sealed record GetLessonChargesQuery(UserAccountId AccountId, Guid? AgreementId, Guid? LessonId)
    : IRequest<IReadOnlyList<LessonChargeResponse>>;
