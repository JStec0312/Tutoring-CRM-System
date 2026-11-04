using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Lessons;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.GetTotalTeachingTime;

public sealed class GetTotalTeachingTimeHandler(TutoringDbContext dbContext)
    : IRequestHandler<GetTotalTeachingTimeQuery, TotalTeachingTimeResponse>
{
    public async Task<TotalTeachingTimeResponse> Handle(GetTotalTeachingTimeQuery request,
        CancellationToken cancellationToken)
    {
        var completedLessons = dbContext.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.Status == LessonStatus.Completed
                && lesson.Agreement.Tutor.UserAccountId == request.UserAccountId)
            .Select(lesson => new { lesson.TimeSlot.StartsAtUtc, lesson.TimeSlot.EndsAtUtc });

        decimal totalTicks = 0;
        await foreach (var lesson in completedLessons.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            totalTicks += (lesson.EndsAtUtc - lesson.StartsAtUtc).Ticks;
        }

        return new TotalTeachingTimeResponse(totalTicks / TimeSpan.TicksPerSecond);
    }
}
