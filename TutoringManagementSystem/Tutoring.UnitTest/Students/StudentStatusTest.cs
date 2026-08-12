using Tutoring.Domain.Students;

namespace Tutoring.UnitTest.Students;

public class StudentStatusTest
{
    [Theory]
    [InlineData(StudentStatus.Active, 0)]
    [InlineData(StudentStatus.Inactive, 1)]
    [InlineData(StudentStatus.Archived, 2)]
    public void StudentStatus_withKnownValue_ShouldHaveExpectedNumericValue(StudentStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }
}
