using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Lessons;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.GetCompletedLessonCount;

public sealed class GetCompletedLessonCountHandler(TutoringDbContext dbContext)
    : IRequestHandler<GetCompletedLessonCountQuery, CompletedLessonCountResponse>
{
    public async Task<CompletedLessonCountResponse> Handle(GetCompletedLessonCountQuery request,
        CancellationToken cancellationToken)
    {
        var count = await dbContext.Lessons.CountAsync(lesson =>
            lesson.Status == LessonStatus.Completed && lesson.Agreement.Tutor.UserAccountId == request.UserAccountId,
            cancellationToken);
        return new CompletedLessonCountResponse(count);
    }
}
