using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;

namespace Tutoring.UnitTest.Lessons;

public sealed class LessonPlannedTopicTests
{
    [Fact]
    public void SetPlannedTopic_TrimsAndClearsTopic()
    {
        var start = DateTimeOffset.UnixEpoch.AddDays(1);
        var lesson = new Lesson(TutoringAgreementId.New(), new TimeSlot(start, start.AddHours(1)), start.AddDays(-1),
            plannedTopic: "  Fractions  ");

        Assert.Equal("Fractions", lesson.PlannedTopic);
        Assert.Throws<ArgumentException>(() => lesson.SetPlannedTopic(new string('x', 201)));
        Assert.Equal("Fractions", lesson.PlannedTopic);
        lesson.SetPlannedTopic(" ");
        Assert.Null(lesson.PlannedTopic);
    }
}
