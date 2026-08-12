using Tutoring.Domain.Identity;

namespace Tutoring.UnitTest.Identity;

public class UserAccountIdTest
{
    [Fact]
    public void New_ShouldGenerateNonEmptyValue()
    {
        var id = UserAccountId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void New_ShouldGenerateUniqueValues()
    {
        var first = UserAccountId.New();
        var second = UserAccountId.New();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Create_withGuidValue_ShouldSetValue()
    {
        var guid = Guid.NewGuid();

        var id = new UserAccountId(guid);

        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void Equals_withSameValue_ShouldBeEqual()
    {
        var guid = Guid.NewGuid();

        var first = new UserAccountId(guid);
        var second = new UserAccountId(guid);

        Assert.Equal(first, second);
    }
}
