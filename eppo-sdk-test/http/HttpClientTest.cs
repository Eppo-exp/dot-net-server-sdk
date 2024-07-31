
using System.Net;
using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.http;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test.http;

[TestFixture]
public class HttpClientTest
{
    private WireMockServer? MockServer;
    private EppoHttpClient? Client;

    private string BaseUrl;

    [OneTimeSetUp]
    public void Setup()
    {
        SetupMockServer();
    }

    private void SetupMockServer()
    {
        MockServer = WireMockServer.Start();
        var response = GetMockFlagConfig();
        Console.WriteLine($"MockServer started at: {MockServer.Urls[0]}");

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

        BaseUrl = MockServer?.Urls[0]!;
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
        Client = new EppoHttpClient("none", "test", "1", BaseUrl);

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
    public void ShouldIndicateConfigModified() { 
            Client = new EppoHttpClient("none", "test", "1", BaseUrl);

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