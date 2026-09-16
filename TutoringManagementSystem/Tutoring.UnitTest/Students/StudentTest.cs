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
        var studentDisplayname = new StudentDisplayName("TestDisplayName");
        var student = new Student(userAccountId,  studentDisplayname, createdAtUtc);

        Assert.Equal(userAccountId, student.UserAccountId);
        Assert.Equal(StudentStatus.Active, student.Status);
        Assert.NotEqual(Guid.Empty, student.Id.Value);
        Assert.Equal(createdAtUtc, student.CreatedAtUtc);
        Assert.Equal(studentDisplayname, student.DisplayName);
    }

    [Fact]
    public void Create_withoutUserAccountId_ShouldSucceedAsManagedStudent()
    {
        var createdAtUtc = new DateTimeOffset(2026, 8, 21, 18, 0, 0, TimeSpan.Zero);
        var studentDisplayname = new StudentDisplayName("ManagedStudent");

        var student = new Student(studentDisplayname, createdAtUtc);

        Assert.Null(student.UserAccountId);
        Assert.Null(student.Account);
        Assert.Equal(StudentStatus.Active, student.Status);
        Assert.NotEqual(Guid.Empty, student.Id.Value);
        Assert.Equal(createdAtUtc, student.CreatedAtUtc);
        Assert.Equal(studentDisplayname, student.DisplayName);
    }
}
