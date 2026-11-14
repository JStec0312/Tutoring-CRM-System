using MediatR;
using Tutoring.Domain.Identity;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.Api.Features.LearningMaterials.GetLearningMaterials;

public sealed record GetLearningMaterialsQuery(UserAccountId AccountId, TutoringAgreementId AgreementId)
    : IRequest<IReadOnlyList<LearningMaterialResponse>>;
