using eppo_sdk.dto;
using eppo_sdk.helpers;

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
            new Subject(),
            new AppDetails().AsDict(),
            new Dictionary<string, string>());
        Assert.That(assignmentLogData.Experiment, Is.EqualTo("feature-flag-allocation"));
    }
}
