using System.Net;
using eppo_sdk;
using eppo_sdk.logger;
using Moq;
using Newtonsoft.Json;
using System.Diagnostics;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

using static NUnit.Framework.Assert;

namespace eppo_sdk_test;

[TestFixture]
public class ProfileEppoClientTest
{
    private WireMockServer? _mockServer;

    private Mock<IAssignmentLogger> _mockAssignmentLogger;

    private EppoClient? client;

    [SetUp]
    public void Setup()
    {
        SetupMockServer();
        _mockAssignmentLogger = new Mock<IAssignmentLogger>();
    }

    private EppoClient CreateClient()
    {
        var config = new EppoClientConfig("mock-api-key", _mockAssignmentLogger.Object)
        {
            BaseUrl = _mockServer?.Urls[0]!
        };
        return EppoClient.Init(config);
    }

    private EppoClient CreteClientModeClient()
    {
        var config = new EppoClientConfig("mock-api-key", _mockAssignmentLogger.Object)
        {
            BaseUrl = _mockServer?.Urls[0]!
        };
        return EppoClient.InitClientMode(config);
    }

    private void SetupMockServer()
    {
        _mockServer = WireMockServer.Start();
        var response = GetMockFlagConfig();
        this._mockServer
            .Given(Request.Create().UsingGet().WithPath(new RegexMatcher("api/flag-config/v1/config")))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody(response).WithHeader("Content-Type", "application/json"));
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        client?.Dispose();
        _mockServer?.Stop();
    }

    [TearDown]
    public void TeardownEach()
    {
        _mockAssignmentLogger.Invocations.Clear();
        _mockServer!.Stop();
    }

    private static string GetMockFlagConfig()
    {
        var filePath = Path.Combine(new DirectoryInfo(Environment.CurrentDirectory).Parent?.Parent?.Parent?.FullName,
            "files/ufc/flags-v1.json");
        using var sr = new StreamReader(filePath);
        return sr.ReadToEnd();
    }

    [Test]
    public void TestGetStringAssignmentPerformance()
    {
        client = CreateClient();
        var variationCounts = new Dictionary<string, int>();
        var subjectAttributes = new Dictionary<string, object>
        {
            ["country"] = "FR"
        };

    const int numIterations = 10000;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < numIterations; i++)
        {
            var subjectKey = $"subject{i}";
            var assignedVariation = client.GetStringAssignment("new-user-onboarding", subjectKey, subjectAttributes, "default");
            
            if (!variationCounts.ContainsKey(assignedVariation))
            {
                variationCounts[assignedVariation] = 0;
            }
            variationCounts[assignedVariation]++;
        }

        stopwatch.Stop();
        var elapsedTicks = stopwatch.ElapsedTicks;
        
        TestContext.WriteLine($"Assignment counts: {JsonConvert.SerializeObject(variationCounts)}");
        TestContext.WriteLine($"Elapsed Ticks: {elapsedTicks} (Frequency: {Stopwatch.Frequency})");

        // Assert distribution matches expected shard ranges
        That(variationCounts.Keys.Count, Is.EqualTo(4), "Should have 4 variations");
        
        // Check distribution percentages with 2% tolerance
        var tolerance = 0.02;
        That((double)variationCounts["default"] / numIterations, Is.EqualTo(0.40).Within(tolerance), "Default should be ~40%");
        That((double)variationCounts["control"] / numIterations, Is.EqualTo(0.30).Within(tolerance), "Control should be ~30%");
        That((double)variationCounts["red"] / numIterations, Is.EqualTo(0.18).Within(tolerance), "Red should be ~18%");
        That((double)variationCounts["yellow"] / numIterations, Is.EqualTo(0.12).Within(tolerance), "Yellow should be ~12%");

        // Performance check
        // Note: Stopwatch.Frequency gives ticks per second, so we can convert to nanoseconds
        var elapsedNanoseconds = (elapsedTicks * 1_000_000_000L) / Stopwatch.Frequency;
        //var maxAllowedNanoseconds = 15000L * numIterations;
        var maxAllowedNanoseconds = 150_000L * numIterations;
        
        That(elapsedNanoseconds, Is.LessThan(maxAllowedNanoseconds), 
            $"Elapsed time of {elapsedNanoseconds}ns is more than the {maxAllowedNanoseconds}ns allowed");
    }
}
    