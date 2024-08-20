using eppo_sdk.dto;

namespace eppo_sdk_test.helpers;

public class AssignmentLogDataTest
{
    [Test]
    public void ShouldBuildExperimentKey()
    {
        var assignmentLogData = new AssignmentLogData(
            "feature-flag",
            "allocation",
            "variation",
            "subject",
            new Subject(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>());
        Assert.That(assignmentLogData.Experiment, Is.EqualTo("feature-flag-allocation"));
    }
}
