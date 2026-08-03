using Tutoring.Domain.Common;

namespace Tutoring.Domain.Lessons;

public sealed class LessonNote
    : Entity<LessonNoteId>
{
    private LessonNote()
    {
    }

    public LessonId LessonId { get; private set; }

    public string Topic { get; private set; } = null!;

    public string CoveredTopics { get; private set; } = null!;

    public string StudentProgress { get; private set; } = null!;
}
