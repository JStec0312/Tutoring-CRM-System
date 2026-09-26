using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.UpdateStudentHourlyRate;

public sealed record UpdateStudentHourlyRateCommand(
    UserAccountId UserAccountId,
    Guid StudentId,
    decimal? HourlyRate,
    RequestMetadata RequestMetadata)
    : IRequest;
