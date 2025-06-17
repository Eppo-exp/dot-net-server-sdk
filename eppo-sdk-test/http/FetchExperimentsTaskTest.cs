using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.helpers;
using eppo_sdk.http;
using eppo_sdk.store;
using eppo_sdk.tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework.Internal;

namespace eppo_sdk_test.http;

public class FetchExperimentsTaskTest
{
    [Test]
    public void ShouldFailGracefully()
    {
        Mock<IConfigurationRequester> mockConfig = new Mock<IConfigurationRequester>();

        // Throw an exception when the config is loaded.
        mockConfig
            .Setup(mc => mc.FetchAndActivateConfiguration())
            .Throws(new SystemException("Error loading"));

        FetchExperimentsTask fet = new FetchExperimentsTask(mockConfig.Object, 250, 0);

        // Sleep one second to await 2+ fetch attempts.
        // If the FetchExperimentsTask encounters an uncaught exception, it will fail the test.
        Thread.Sleep(1000);

        fet.Dispose();
    }
}
