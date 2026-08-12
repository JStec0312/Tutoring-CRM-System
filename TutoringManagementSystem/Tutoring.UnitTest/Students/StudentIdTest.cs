using Tutoring.Domain.Students;

namespace Tutoring.UnitTest.Students;

public class StudentIdTest
{
    [Fact]
    public void New_ShouldGenerateNonEmptyValue()
    {
        var id = StudentId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void New_ShouldGenerateUniqueValues()
    {
        var first = StudentId.New();
        var second = StudentId.New();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Create_withGuidValue_ShouldSetValue()
    {
        var guid = Guid.NewGuid();

        var id = new StudentId(guid);

        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void Equals_withSameValue_ShouldBeEqual()
    {
        var guid = Guid.NewGuid();

        var first = new StudentId(guid);
        var second = new StudentId(guid);

        Assert.Equal(first, second);
    }
}
