
using System.Net;
using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.http;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using FluentAssertions;
using WireMock.FluentAssertions;
using WireMock.Settings;

using static NUnit.Framework.Assert;

namespace eppo_sdk_test.http;

[TestFixture]
public class HttpClientTest
{
    private WireMockServer MockServer;
    private EppoHttpClient? Client;

    private string BaseUrl;

    IReadOnlyDictionary<string, string> requestParams;

    public HttpClientTest()
    {
        MockServer = WireMockServer.Start();
    }

    [OneTimeSetUp]
    public void Setup()
    {
        SetupMockServer();
        requestParams = new Dictionary<string, string>
        {
            {"sdkName", "dotnetTest"},
            {"sdkVersion", "9.9.9"}
        }.AsReadOnly();
    }

    [OneTimeTearDown]
    public void ShutdownServer()
    {
        MockServer?.Stop();
    }

    private void SetupMockServer()
    {
        var response = GetMockFlagConfig();
        Console.WriteLine($"MockServer started at: {MockServer.Urls.First()}");

        // ETags
        var currentETag = "CURRENT";

        this.MockServer
            .Given(Request.Create().UsingGet().WithPath(new RegexMatcher("flag-config/v1/config")))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(response)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("ETag", currentETag));

        this.MockServer
            .Given(Request.Create()
                .UsingGet()
                .WithHeader("IF-NONE-MATCH", currentETag)
                .WithPath(new RegexMatcher("flag-config/v1/config")))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NotModified)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("ETag", currentETag));

        BaseUrl = MockServer?.Urls.First()!;
    }

    private static string GetMockFlagConfig()
    {
        var filePath = Path.Combine(new DirectoryInfo(Environment.CurrentDirectory).Parent?.Parent?.Parent?.FullName,
            "files/ufc/bandit-flags-v1.json");
        using var sr = new StreamReader(filePath);
        return sr.ReadToEnd();
    }

    [Test]
    public void ShouldFetchAndParseConfig()
    {
        Client = new EppoHttpClient("none", requestParams, BaseUrl);

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
    public void ShouldSendAdditionalParams()
    {
        Client = new EppoHttpClient("none", requestParams, BaseUrl);

        Client.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT);

        // Query param values hardcoded in `requestParams`.
        MockServer?.Should()
            .HaveReceivedACall()
            .UsingGet()
            .And.AtUrl($"{BaseUrl}/flag-config/v1/config?apiKey=none&sdkName=dotnetTest&sdkVersion=9.9.9");
    }

    [Test]
    public void ShouldIndicateConfigModified()
    {
        Client = new EppoHttpClient("none", requestParams, BaseUrl);

        var ufcResponse = Client.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT);

        var shouldBeUnmodifiedResponse = Client.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT, ufcResponse.VersionIdentifier);

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

}