using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.helpers;
using eppo_sdk.http;
using eppo_sdk.store;
using eppo_sdk.tasks;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using NUnit.Framework.Internal;

namespace eppo_sdk_test.http;

public class FetchExperimentsTaskTest
{
    [Test]
    public void ShouldFailGracefully()
    {
        var callCount = 0;
        Mock<IConfigurationRequester> mockConfig = new Mock<IConfigurationRequester>();

        // Throw an exception when the config is loaded.
        mockConfig
            .Setup(mc => mc.FetchAndActivateConfiguration())
            .Callback(() => callCount++)
            .Throws(new SystemException("Error loading"));

        var fakeTimeProvider = new FakeTimeProvider();
        FetchExperimentsTask fet = new FetchExperimentsTask(mockConfig.Object, 250, 0, fakeTimeProvider);

        // Advance time to trigger 2+ fetch attempts.
        // If the FetchExperimentsTask encounters an uncaught exception, it will fail the test.
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(250)); // First timer
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(250)); // Second timer
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(250)); // Third timer

        fet.Dispose();

        // Verify that multiple fetch attempts were made despite exceptions
        Assert.That(callCount, Is.GreaterThanOrEqualTo(2));
    }
}
