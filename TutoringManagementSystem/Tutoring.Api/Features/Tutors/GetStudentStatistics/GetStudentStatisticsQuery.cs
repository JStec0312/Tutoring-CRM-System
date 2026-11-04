using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Students;

namespace Tutoring.Api.Features.Tutors.GetStudentStatistics;

public sealed record GetStudentStatisticsQuery(
    UserAccountId UserAccountId,
    StudentId StudentId,
    DateOnly? From,
    DateOnly? To,
    RequestMetadata RequestMetadata)
    : IRequest<StudentStatisticsResponse>;
