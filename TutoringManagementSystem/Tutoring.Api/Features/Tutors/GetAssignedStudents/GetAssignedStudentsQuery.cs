using MediatR;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetAssignedStudents;

public sealed record GetAssignedStudentsQuery(
    UserAccountId UserAccountId)
    : IRequest<IReadOnlyCollection<AssignedStudentResponse>>;