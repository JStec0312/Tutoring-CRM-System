using MediatR;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetCompletedLessonCount;

public sealed record GetCompletedLessonCountQuery(UserAccountId UserAccountId) : IRequest<CompletedLessonCountResponse>;
