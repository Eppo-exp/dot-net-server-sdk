using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using eppo_sdk;
using eppo_sdk.dto.bandit;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Newtonsoft.Json;
using NUnit.Framework.Constraints;
using RandomDataGenerator.CreditCardValidator;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test;

[TestFixture]
public class BanditClientTest
{
    private EppoClient? _client;

    private const string BANDIT_CONFIG_FILE = "files/ufc/bandit-flags-v1.json";
    private const string BANDIT_MODEL_FILE = "files/ufc/bandit-models-v1.json";
    private WireMockServer? _mockServer;
    private ContextAttributes _subject = new("userID")
    {
        {"account_age", 3},
        {"favourite_colour","red"}
    };
    private readonly Dictionary<string, ContextAttributes> _actions = new()
    {
        {"action1" , new("action1") {
            {"foo", "bar"},
            {"bar", "baz"}
        }},
        {"action2" , new("action2") {
            {"height", 10},
            {"isfast", true}
        }}
    };

    [OneTimeSetUp]
    public void Setup()
    {
        SetupMockServer();
        SetupSubjectMocks();
        var config = new EppoClientConfig("mock-api-key", new TestAssignmentLogger())
        {
            BaseUrl = _mockServer?.Urls[0]!
        };
        _client = EppoClient.Init(config, "BanditClientTest");
    }

    private void SetupSubjectMocks()
    {
        _subject["timeofday"] = "night";
        _subject["loyalty_tier"] = "gold";
    }

    private void SetupMockServer()
    {
        _mockServer = WireMockServer.Start();
        Console.WriteLine($"MockServer started at: {_mockServer.Urls[0]}");
        this._mockServer
            .Given(Request.Create().UsingGet().WithPath(new RegexMatcher("flag-config/v1/config")))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody(GetMockBanditConfig()).WithHeader("Content-Type", "application/json"));
        this._mockServer
            .Given(Request.Create().UsingGet().WithPath(new RegexMatcher("flag-config/v1/bandits")))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody(GetMockBanditModelConfig()).WithHeader("Content-Type", "application/json"));
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        // EppoClient.DeInit();
        _mockServer?.Stop();
    }

    private static string GetMockBanditConfig() => GetMockConfig(BANDIT_CONFIG_FILE);
    private static string GetMockBanditModelConfig() => GetMockConfig(BANDIT_MODEL_FILE);

    private static string GetMockConfig(string resourceFile)
    {
        var filePath = Path.Combine(new DirectoryInfo(Environment.CurrentDirectory).Parent?.Parent?.Parent?.FullName,
            resourceFile);
        using var sr = new StreamReader(filePath);
        return sr.ReadToEnd();
    }

    [Test]
    public void ShouldReturnDefaultValue()
    {
        var client = EppoClient.GetInstance();
        var result = client.GetBanditAction("unknownflag", _subject, _actions, "defaultVariation");
        Multiple(() =>
        {
            That(result, Is.Not.Null);
            That(result.Variation, Is.EqualTo("defaultVariation"));
            That(result.Action, Is.Null);
        });
    }

    [Test, TestCaseSource(nameof(GetTestAssignmentData))]
    public void ShouldAssignBandits(BanditTestCase banditTestCase)
    {
        var client = EppoClient.GetInstance();

        foreach (var subject in banditTestCase.Subjects)
        {
            var expected = subject.Assignment;
            Dictionary<string, ContextAttributes> actions =
                subject.Actions.ToDictionary(atr => atr.ActionKey, atr => new ContextAttributes(atr.ActionKey, atr.CategoricalAttributes, atr.NumericalAttributes));


            var result = client.GetBanditAction(
                banditTestCase.Flag,
                subject.SubjectKey,
                subject.SubjectAttributes.AsDict(),
                actions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.AsDict()),
                banditTestCase.DefaultValue
            );
            Assert.Multiple(() =>
            {
                Assert.That(result.Variation, Is.EqualTo(expected.Variation), "Unexpected assignment in " + banditTestCase.TestCaseFile);
                Assert.That(result.Action, Is.EqualTo(expected.Action), "Unexpected assignment in " + banditTestCase.TestCaseFile);
            });
        }
    }


    static List<BanditTestCase> GetTestAssignmentData()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory).Parent?.Parent?.Parent?.FullName;
        var files = Directory.EnumerateFiles($"{dir}/files/ufc/bandit-tests", "*.json");
        var testCases = new List<BanditTestCase>() { };
        foreach (var file in files)
        {
            var atc = JsonConvert.DeserializeObject<BanditTestCase>(File.ReadAllText(file))!;
            atc.TestCaseFile = file;
            testCases.Add(atc);
        }
        return testCases;
    }
}


public record BanditTestCase(string Flag,
                             string DefaultValue,
                             List<BanditSubjectTestRecord> Subjects)
{
    public string? TestCaseFile;

}
public record BanditSubjectTestRecord(string SubjectKey,
                                      SubjectAttributeSet SubjectAttributes,
                                      ActionTestRecord[] Actions,
                                      BanditResult Assignment)
{
}

public record ActionTestRecord(string ActionKey,
                               Dictionary<string, string?> CategoricalAttributes,
                               Dictionary<string, double?> NumericalAttributes)
{
}



public record SubjectAttributeSet
{
    [JsonProperty("numeric_attributes")]
    public IDictionary<string, double?>? NumericAttributes = new Dictionary<string, double?>();
    [JsonProperty("categorical_attributes")]
    public IDictionary<string, string?>? CategoricalAttributes = new Dictionary<string, string?>();

    public IDictionary<string, object?> AsDict()
    {
        var combinedDictionary = new Dictionary<string, object?>();
        if (NumericAttributes != null)
        {
            foreach (var kvp in NumericAttributes)
            {
                combinedDictionary.Add(kvp.Key, kvp.Value);
            }
        }
        if (CategoricalAttributes != null)
        {
            foreach (var kvp in CategoricalAttributes)
            {
                combinedDictionary.Add(kvp.Key, kvp.Value);
            }
        }
        return combinedDictionary;
    }

}