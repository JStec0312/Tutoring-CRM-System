using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.InviteStudent;

public sealed record InviteStudentCommand(
    UserAccountId UserAccountId,
    string Email,
    string Title,
    string Subject,
    decimal? HourlyRate,
    RequestMetadata RequestMetadata)
    : IRequest<InviteStudentResponse>;