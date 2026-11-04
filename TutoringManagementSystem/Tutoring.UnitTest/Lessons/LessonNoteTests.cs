using Tutoring.Domain.Lessons;

namespace Tutoring.UnitTest.Lessons;

public sealed class LessonNoteTests
{
    [Fact]
    public void Constructor_AcceptsMaximumLengths()
    {
        var note = new LessonNote(LessonId.New(), new string('t', 200), new string('c', 2000), new string('p', 2000));

        Assert.Equal(200, note.Topic.Length);
        Assert.Equal(2000, note.CoveredTopics.Length);
        Assert.Equal(2000, note.StudentProgress.Length);
    }

    [Fact]
    public void InvalidUpdate_LeavesAllFieldsIntact()
    {
        var note = new LessonNote(LessonId.New(), "Topic", "Covered", "Progress");

        Assert.Throws<InvalidLessonNoteException>(() => note.Update("New topic", "New coverage", null));
        Assert.Equal(("Topic", "Covered", "Progress"), (note.Topic, note.CoveredTopics, note.StudentProgress));
    }

    [Fact]
    public void Constructor_RejectsTopicOverMaximumLength()
    {
        Assert.Throws<InvalidLessonNoteException>(() =>
            new LessonNote(LessonId.New(), new string('t', 201), "Covered", "Progress"));
    }
}
