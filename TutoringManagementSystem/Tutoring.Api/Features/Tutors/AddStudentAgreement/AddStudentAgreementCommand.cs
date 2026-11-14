using MediatR;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Students;

namespace Tutoring.Api.Features.Tutors.AddStudentAgreement;

public sealed record AddStudentAgreementCommand(
    UserAccountId AccountId, StudentId StudentId, string Title, string Subject, decimal? HourlyRate) : IRequest<Guid>;
