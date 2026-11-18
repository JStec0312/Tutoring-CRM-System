using MediatR;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Students.GetStudentAgreements;

public sealed record GetStudentAgreementsQuery(UserAccountId AccountId) : IRequest<IReadOnlyList<StudentAgreementResponse>>;
