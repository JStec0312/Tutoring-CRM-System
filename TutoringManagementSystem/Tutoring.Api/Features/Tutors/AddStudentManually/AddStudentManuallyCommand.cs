using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.AddStudentManually;

public sealed record AddStudentManuallyCommand(
    UserAccountId UserAccountId,
    string DisplayName,
    string Title,
    string Subject,
    decimal? HourlyRate,
    RequestMetadata RequestMetadata)
    : IRequest<AddStudentManuallyResponse>;