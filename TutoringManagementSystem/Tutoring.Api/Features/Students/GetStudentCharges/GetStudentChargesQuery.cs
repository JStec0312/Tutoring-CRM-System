using MediatR;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Students.GetStudentCharges;

public sealed record GetStudentChargesQuery(UserAccountId AccountId, Guid AgreementId) : IRequest<IReadOnlyList<StudentChargeResponse>>;
