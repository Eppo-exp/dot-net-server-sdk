using System.Net;
using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.http;
using FluentAssertions;
using WireMock.FluentAssertions;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test.http;

[TestFixture]
public class HttpClientTest
{
    private WireMockServer _mockServer;

    private string BaseUrl;

    private WireMockServer MockServer
    {
        get => _mockServer!;
    }

    [OneTimeSetUp]
    public void Setup()
    {
        SetupMockServer();
    }

    [OneTimeTearDown]
    public void ShutdownServer()
    {
        MockServer.Stop();
    }

    private void SetupMockServer()
    {
        _mockServer = WireMockServer.Start();
        var response = GetMockFlagConfig();
        Console.WriteLine($"MockServer started at: {MockServer.Urls.First()}");

        // ETags
        var currentETag = "CURRENT";

        MockServer
            .Given(Request.Create().UsingGet().WithPath(new RegexMatcher("flag-config/v1/config")))
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithBody(response)
                    .WithHeader("Content-Type", "application/json")
                    .WithHeader("ETag", currentETag)
            );

        MockServer
            .Given(
                Request
                    .Create()
                    .UsingGet()
                    .WithHeader("IF-NONE-MATCH", currentETag)
                    .WithPath(new RegexMatcher("flag-config/v1/config"))
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.NotModified)
                    .WithHeader("Content-Type", "application/json")
                    .WithHeader("ETag", currentETag)
            );

        BaseUrl = MockServer.Urls.First()!;
    }

    private static string GetMockFlagConfig()
    {
        var filePath = Path.Combine(
            new DirectoryInfo(Environment.CurrentDirectory).Parent?.Parent?.Parent?.FullName,
            "files/ufc/bandit-flags-v1.json"
        );
        using var sr = new StreamReader(filePath);
        return sr.ReadToEnd();
    }

    [Test]
    public void ShouldFetchAndParseConfig()
    {
        var Client = CreatClient(BaseUrl);

        var ufcResponse = Client.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT);

        Assert.Multiple(() =>
        {
            That(ufcResponse, Is.Not.Null);
            That(ufcResponse.IsModified, Is.True);
            That(ufcResponse.VersionIdentifier, Is.EqualTo("CURRENT"));
            That(ufcResponse.Resource, Is.Not.Null);
        });
    }

    [Test]
    public void ShouldSendSDKParams()
    {
        var Client = CreatClient(BaseUrl);

        Client.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT);

        MockServer
            .Should()
            .HaveReceivedACall()
            .UsingGet()
            .And.AtUrl(
                $"{BaseUrl}/flag-config/v1/config?apiKey=none&sdkName=dotnetTest&sdkVersion=9.9.9"
            );
    }

    [Test]
    public void ShouldIndicateConfigModified()
    {
        var Client = CreatClient(BaseUrl);

        var ufcResponse = Client.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT);

        var shouldBeUnmodifiedResponse = Client.Get<FlagConfigurationResponse>(
            Constants.UFC_ENDPOINT,
            ufcResponse.VersionIdentifier
        );

        Assert.Multiple(() =>
        {
            That(ufcResponse, Is.Not.Null);
            That(ufcResponse.IsModified, Is.True);
            That(ufcResponse.VersionIdentifier, Is.EqualTo("CURRENT"));
            That(ufcResponse.Resource, Is.Not.Null);

            That(shouldBeUnmodifiedResponse, Is.Not.Null);
            That(shouldBeUnmodifiedResponse.IsModified, Is.False);
            That(shouldBeUnmodifiedResponse.VersionIdentifier, Is.EqualTo("CURRENT"));
            That(shouldBeUnmodifiedResponse.Resource, Is.Null);
        });
    }

    private static EppoHttpClient CreatClient(
        String baseUrl,
        int timeoutMillis = Constants.REQUEST_TIMEOUT_MILLIS
    )
    {
        return new EppoHttpClient("none", "dotnetTest", "9.9.9", baseUrl, timeoutMillis);
    }

    [Test]
    public void ShouldTimeoutOnSlowResponse()
    {
        MockServer
            .Given(Request.Create().UsingGet().WithPath(new RegexMatcher("slow-endpoint")))
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithBody("{\"test\": \"data\"}")
                    .WithHeader("Content-Type", "application/json")
                    .WithDelay(TimeSpan.FromMilliseconds(2000))
            );

        var clientWithShortTimeout = CreatClient(BaseUrl, 500);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = clientWithShortTimeout.Get<object>("/slow-endpoint");
        stopwatch.Stop();

        // The timeout should cause a null resource and take roughly the timeout duration
        var timeoutOccurred = stopwatch.ElapsedMilliseconds < 600; // Much less than 2000ms delay

        Assert.Multiple(() =>
        {
            Assert.That(timeoutOccurred, Is.True, "Request should have timed out and completed quickly");
            Assert.That(response.Resource, Is.Null, "Response resource should be null due to timeout");
        });
    }

    [Test]
    public void ShouldSucceedWithSufficientTimeout()
    {
        MockServer
            .Given(Request.Create().UsingGet().WithPath(new RegexMatcher("medium-slow-endpoint")))
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithBody("{\"test\": \"data\"}")
                    .WithHeader("Content-Type", "application/json")
                    .WithDelay(TimeSpan.FromMilliseconds(500))
            );

        var clientWithLongTimeout = CreatClient(BaseUrl, 2000);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = clientWithLongTimeout.Get<object>("/medium-slow-endpoint");
        stopwatch.Stop();

        // Should succeed with sufficient timeout
        var timeoutDidNotOccur = stopwatch.ElapsedMilliseconds >= 500;   

        Assert.Multiple(() =>
        {
            Assert.That(timeoutDidNotOccur, Is.True, "Request should have completed normally without timeout");
            Assert.That(response.Resource, Is.Not.Null, "Response resource should not be null with sufficient timeout");
        });
    }
}
