using eppo_sdk.dto;

namespace eppo_sdk_test.helpers;

public class AssignmentLogDataTest
{
    [Test]
    public void ShouldReturnAssignmentLogData()
    {
        var assignmentLogData = new AssignmentLogData(
            "feature-flag",
            "allocation",
            "variation",
            "subject",
            new Subject());
        Assert.That(assignmentLogData.experiment, Is.EqualTo("feature-flag-allocation"));
    }
}
