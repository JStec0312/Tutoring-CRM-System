using Tutoring.Domain.Identity;
using Tutoring.Domain.Students;

namespace Tutoring.UnitTest.Students;

public class StudentTest
{
    [Fact]
    public void Create_withValidUserAccountId_ShouldSucceed()
    {
        var userAccountId = UserAccountId.New();
        var createdAtUtc = new DateTimeOffset(2026, 8, 21, 18, 0, 0, TimeSpan.Zero);

        var student = new Student(userAccountId, createdAtUtc);

        Assert.Equal(userAccountId, student.UserAccountId);
        Assert.Equal(StudentStatus.Active, student.Status);
        Assert.NotEqual(Guid.Empty, student.Id.Value);
        Assert.Equal(createdAtUtc, student.CreatedAtUtc);
    }
}
