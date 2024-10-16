using eppo_sdk.dto;

namespace eppo_sdk_test.helpers;

public class AssignmentLogDataTest
{
    private AssignmentLogData assignmentLogData;

    [SetUp]
    public void Setup()
    {
        assignmentLogData = new AssignmentLogData(
            "feature-flag",
            "allocation",
            "variation",
            "subject",
            new Subject(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>());
    }

    [Test]
    public void ShouldBuildExperimentKey()
    {
        Assert.That(assignmentLogData.Experiment, Is.EqualTo("feature-flag-allocation"));
    }

    [Test]
    public void ShouldSetAssignmentTimestamp()
    {
        Assert.That(assignmentLogData.Timestamp, Is.EqualTo(DateTime.UtcNow).Within(1).Milliseconds);
    }
}
