using Tutoring.Domain.Identity;
using Tutoring.Domain.Students;

namespace Tutoring.UnitTest.Students;

public class StudentTest
{
    [Fact]
    public void Create_withValidUserAccountId_ShouldSucceed()
    {
        var userAccountId = UserAccountId.New();
        var beforeCreation = DateTimeOffset.UtcNow;

        var student = new Student(userAccountId);

        var afterCreation = DateTimeOffset.UtcNow;
        Assert.Equal(userAccountId, student.UserAccountId);
        Assert.Equal(StudentStatus.Active, student.Status);
        Assert.NotEqual(Guid.Empty, student.Id.Value);
        Assert.InRange(student.CreatedAtUtc, beforeCreation, afterCreation);
    }
}
