using eppo_sdk.store;
using eppo_sdk.tasks;
using Moq;

namespace eppo_sdk_test.tasks;

public class FetchExperimentsTaskTest
{
    [Test]
    public void ShouldRunTimerAtConfiguredIntervals()
    {
        var count = 0;
        var mockConfigStore = new Mock<IConfigurationStore>();
        mockConfigStore.Setup(x => x.FetchExperimentConfiguration()).Callback(() =>
        {
            count++;
        });
        
        var task = new FetchExperimentsTask(mockConfigStore.Object, 500, 10);
        Thread.Sleep(1100); // wait for more than 1s to avoid flaky tests.
        Assert.That(count, Is.EqualTo(2));
    }
}