using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Calendar.GetCalendarLessons;

public sealed record GetCalendarLessonsQuery(
    UserAccountId UserAccountId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    RequestMetadata RequestMetadata)
    : IRequest<IReadOnlyCollection<CalendarLessonResponse>>;