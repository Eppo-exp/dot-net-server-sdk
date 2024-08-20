using eppo_sdk.http;
using eppo_sdk.tasks;
using Moq;

namespace eppo_sdk_test.tasks;

public class FetchExperimentsTaskTest
{
    [Test]
    public void ShouldRunTimerAtConfiguredIntervals()
    {
        var count = 0;
        var mockConfig = new Mock<IConfigurationRequester>();
        mockConfig.Setup(x => x.LoadConfiguration()).Callback(() =>
        {
            count++;
        });
        
        var task = new FetchExperimentsTask(mockConfig.Object, 500, 10);
        Thread.Sleep(1100); // wait for more than 1s to avoid flaky tests.
        Assert.That(count, Is.EqualTo(2));
    }
}