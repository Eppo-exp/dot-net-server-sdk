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
        mockConfig
            .Setup(x => x.FetchAndActivateConfiguration())
            .Callback(() =>
            {
                count++;
            });

        // Use a shorter interval for faster testing
        var task = new FetchExperimentsTask(mockConfig.Object, 200, 10);

        // Wait for 2.5 intervals to ensure we get at least 2 calls (initial + 1 interval)
        Thread.Sleep(500);

        // Verify at least 2 calls (initial call + at least one timer call)
        Assert.That(count, Is.GreaterThanOrEqualTo(2));
    }
}
