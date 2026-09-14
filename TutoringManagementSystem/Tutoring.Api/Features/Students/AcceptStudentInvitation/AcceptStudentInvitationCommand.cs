using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Students.AcceptStudentInvitation;

public sealed record AcceptStudentInvitationCommand(
    UserAccountId UserAccountId,
    string Token,
    RequestMetadata RequestMetadata)
    : IRequest<AcceptStudentInvitationResponse>;