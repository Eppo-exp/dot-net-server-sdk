using eppo_sdk.http;
using eppo_sdk.tasks;
using Microsoft.Extensions.Time.Testing;
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
        var fakeTimeProvider = new FakeTimeProvider();
        var task = new FetchExperimentsTask(mockConfig.Object, 200, 10, fakeTimeProvider);

        // Advance time to trigger the first timer callback: exactly 200ms (no jitter on initial delay)
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(200));

        // Advance time to trigger the second timer callback: 191-199ms (with jitter), so 199ms covers worst case
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(199));

        // Verify at least 2 calls (initial call + at least one timer call)
        Assert.That(count, Is.GreaterThanOrEqualTo(2));
    }
}
