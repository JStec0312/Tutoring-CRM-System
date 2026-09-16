using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.UpdateStudentDetails;

public sealed record UpdateStudentDetailsCommand(
    UserAccountId UserAccountId,
    Guid StudentId,
    string Subject,
    decimal? HourlyRate,
    string? ContactEmail,
    string? ContactPhoneNumber,
    string? Notes,
    RequestMetadata RequestMetadata)
    : IRequest;
