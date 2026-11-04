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

    public LessonNote(LessonId lessonId, string? topic, string? coveredTopics, string? studentProgress)
    {
        LessonId = lessonId;
        Update(topic, coveredTopics, studentProgress);
    }

    public void Update(string? topic, string? coveredTopics, string? studentProgress)
    {
        var validTopic = Validate(topic, 200, nameof(Topic));
        var validCoveredTopics = Validate(coveredTopics, 2000, nameof(CoveredTopics));
        var validStudentProgress = Validate(studentProgress, 2000, nameof(StudentProgress));

        Topic = validTopic;
        CoveredTopics = validCoveredTopics;
        StudentProgress = validStudentProgress;
    }

    private static string Validate(string? value, int maxLength, string field)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > maxLength)
            throw new InvalidLessonNoteException(field, maxLength);

        return trimmed;
    }
}
