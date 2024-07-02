using System.Net;
using eppo_sdk;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.logger;
using Moq;
using Newtonsoft.Json;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test;

[TestFixture]
public class BanditClientTest
{
    private const string BANDIT_CONFIG_FILE = "files/ufc/bandit-flags-v1.json";
    private const string BANDIT_MODEL_FILE = "files/ufc/bandit-models-v1.json";
    private WireMockServer? _mockServer;
    private ContextAttributes _subject = new("subject_key")
    {
        {"account_age", 3},
        {"favourite_colour", "red"},
        {"age", 30},
        {"country", "UK"}
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
    }

    private EppoClient CreateClient(IAssignmentLogger? logger = null)
    {
        if (logger == null)
        {
            var mockAssignmentLogger = new Mock<IAssignmentLogger>();
            logger = mockAssignmentLogger.Object;
        }
        var config = new EppoClientConfig("mock-api-key", logger)
        {
            BaseUrl = _mockServer?.Urls[0]!
        };
        return EppoClient.Init(config);
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
        _mockServer?.Stop();
    }

    private static string GetMockBanditConfig() => GetMockConfig(BANDIT_CONFIG_FILE);
    private static string GetMockBanditModelConfig() => GetMockConfig(BANDIT_MODEL_FILE);

    private static string GetMockConfig(string resourceFile)
    {
        var directoryPath = new DirectoryInfo(Environment.CurrentDirectory).Parent?.Parent?.Parent?.FullName;
        var filePath = Path.Combine(directoryPath!,
            resourceFile);
        using var sr = new StreamReader(filePath);
        return sr.ReadToEnd();
    }

    [Test]
    public void ShouldReturnDefaultForNonBandit()
    {
        var client = CreateClient();
        var result = client.GetBanditAction("unknownflag", _subject, _actions, "defaultVariation");
        Multiple(() =>
        {
            That(result, Is.Not.Null);
            That(result.Variation, Is.EqualTo("defaultVariation"));
            That(result.Action, Is.Null);
        });
    }

    [Test]
    public void ShouldReturnDefaultForNonBanditFlag()
    {
        var client = CreateClient();
        var result = client.GetBanditAction("a_flag", _subject, new Dictionary<string, ContextAttributes>(), "default_variation");
        Multiple(() =>
        {
            That(result, Is.Not.Null);
            That(result.Variation, Is.EqualTo("default_variation"));
            That(result.Action, Is.Null);
        });
    }


    [Test]
    public void ShouldEvaluateAndLogBanditAndAssignment()
    {
        // #! testing/the/whole

        var mockLogger = new Mock<IAssignmentLogger>();

        List<AssignmentLogData> assignmentLogs = new() { };
        List<BanditLogEvent> banditActionsLogs = new() { };


        mockLogger.Setup(mock => mock.LogAssignment(It.IsAny<AssignmentLogData>()))
            .Callback<AssignmentLogData>(log => assignmentLogs.Add(log));

        mockLogger.Setup(mock => mock.LogBanditAction(It.IsAny<BanditLogEvent>()))
            .Callback<BanditLogEvent>(log => banditActionsLogs.Add(log));

        var client = CreateClient(mockLogger.Object);

        var subjectKey = "subject_key";
        var defaultSubjectAttributes = _subject.AsDict();
        var actions = new Dictionary<string, ContextAttributes>()
        {
            ["adidas"] = new ContextAttributes(
                "adidas")
            {
                ["discount"] = 0.1,
                ["from"] = "germany"
            },
            ["nike"] = new ContextAttributes(
                "nike")
            {
                ["discount"] = 0.2,
                ["from"] = "usa"
            }
        };

        var defaultVariation = "default_variation";


        // Act
        var result = client.GetBanditAction("banner_bandit_flag_uk_only", _subject, actions, defaultVariation);

        Multiple(() =>
        {
            // Assert - Result verification
            That(result.Variation, Is.EqualTo("banner_bandit"));
            That(result.Action == "adidas" || result.Action == "nike", Is.True);

            // Assert - Assignment logger verification
            mockLogger.Verify(logger => logger.LogAssignment(It.IsAny<AssignmentLogData>()), Times.Once());
            That(assignmentLogs, Has.Count.EqualTo(1));

            var assignmentLogStatement = assignmentLogs[0];
            That(assignmentLogStatement, Is.Not.Null);
            var logEvent = assignmentLogStatement!;

            That(logEvent.FeatureFlag, Is.EqualTo("banner_bandit_flag_uk_only"));
            That(logEvent.Variation, Is.EqualTo("banner_bandit"));
            That(logEvent.Subject, Is.EqualTo(subjectKey));

            // Assert - Bandit logger verification
            mockLogger.Verify(logger => logger.LogBanditAction(It.IsAny<BanditLogEvent>()), Times.Once());
            That(banditActionsLogs, Has.Count.EqualTo(1));

            var banditLogStatement = banditActionsLogs[0];

            That(banditLogStatement, Is.Not.Null);
            var banditLog = banditLogStatement!;
            That(banditLog.FlagKey, Is.EqualTo("banner_bandit_flag_uk_only"));
            That(banditLog.BanditKey, Is.EqualTo("banner_bandit"));
            That(banditLog.SubjectKey, Is.EqualTo(subjectKey));
            GreaterOrEqual(banditLog.OptimalityGap, 0);
            GreaterOrEqual(banditLog.ActionProbability, 0);


            That(result.Action, Is.Not.Null);
            var chosenAction = actions[result.Action!];

            That(banditLog.ActionNumericAttributes, Is.Not.Null);
            That(banditLog.ActionCategoricalAttributes, Is.Not.Null);
            AssertDictsEquivalent(banditLog.ActionNumericAttributes!, chosenAction.GetNumeric().AsReadOnly());
            AssertDictsEquivalent(banditLog.ActionCategoricalAttributes!, chosenAction.GetCategorical().AsReadOnly());
        });
    }



    [Test]
    public void ShouldAcceptListOfActionsWithNoAttributes()
    {
        // #! testing/the/whole

        var mockLogger = new Mock<IAssignmentLogger>();

        List<AssignmentLogData> assignmentLogs = new() { };
        List<BanditLogEvent> banditActionsLogs = new() { };


        mockLogger.Setup(mock => mock.LogAssignment(It.IsAny<AssignmentLogData>()))
            .Callback<AssignmentLogData>(log => assignmentLogs.Add(log));

        mockLogger.Setup(mock => mock.LogBanditAction(It.IsAny<BanditLogEvent>()))
            .Callback<BanditLogEvent>(log => banditActionsLogs.Add(log));

        var client = CreateClient(mockLogger.Object);

        var subjectKey = "subject_key";
        var defaultSubjectAttributes = _subject.AsDict();
        var actions = new string[] { "adidas", "nike", "Reebok" };

        var defaultVariation = "default_variation";


        // Act
        var result = client.GetBanditAction("banner_bandit_flag_uk_only", _subject, actions, defaultVariation);

        Multiple(() =>
        {
            // Assert - Result verification
            That(result.Variation, Is.EqualTo("banner_bandit"));
            That(result.Action == "adidas" || result.Action == "nike", Is.True);

            // Assert - Assignment logger verification
            mockLogger.Verify(logger => logger.LogAssignment(It.IsAny<AssignmentLogData>()), Times.Once());
            That(assignmentLogs, Has.Count.EqualTo(1));

            var assignmentLogStatement = assignmentLogs[0];
            That(assignmentLogStatement, Is.Not.Null);
            var logEvent = assignmentLogStatement!;

            That(logEvent.FeatureFlag, Is.EqualTo("banner_bandit_flag_uk_only"));
            That(logEvent.Variation, Is.EqualTo("banner_bandit"));
            That(logEvent.Subject, Is.EqualTo(subjectKey));

            // Assert - Bandit logger verification
            mockLogger.Verify(logger => logger.LogBanditAction(It.IsAny<BanditLogEvent>()), Times.Once());
            That(banditActionsLogs, Has.Count.EqualTo(1));

            var banditLogStatement = banditActionsLogs[0];

            That(banditLogStatement, Is.Not.Null);
            var banditLog = banditLogStatement!;
            That(banditLog.FlagKey, Is.EqualTo("banner_bandit_flag_uk_only"));
            That(banditLog.BanditKey, Is.EqualTo("banner_bandit"));
            That(banditLog.SubjectKey, Is.EqualTo(subjectKey));
            GreaterOrEqual(banditLog.OptimalityGap, 0);
            GreaterOrEqual(banditLog.ActionProbability, 0);


            That(result.Action, Is.Not.Null);
            var chosenAction = actions.Where(a=>a==result.Action);

            That(banditLog.ActionNumericAttributes, Is.Not.Null);
            That(banditLog.ActionCategoricalAttributes, Is.Not.Null);
            AssertDictsEquivalent(banditLog.ActionNumericAttributes!, new Dictionary<string, double>().AsReadOnly());
            AssertDictsEquivalent(banditLog.ActionCategoricalAttributes!, new Dictionary<string, string>().AsReadOnly());
        });
    }


    private void AssertDictsEquivalent<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> a, IReadOnlyDictionary<TKey, TValue> b)
    {
        Multiple(() =>
        {
            That(a.Count, Is.EqualTo(b.Count));
            foreach (var kvp in a)
            {
                That(b[kvp.Key], Is.EqualTo(kvp.Value));
            }
        });
    }

    [Test]
    public void ShouldReturnDefaultForNoActions()
    {

        var mockLogger = new Mock<IAssignmentLogger>();
        var client = CreateClient(mockLogger.Object);

        var result = client.GetBanditAction("banner_bandit_flag", _subject, new Dictionary<string, ContextAttributes>(), "defaultValue");
        Multiple(() =>
        {
            That(result, Is.Not.Null);
            That(result.Variation, Is.EqualTo("defaultValue"));
            That(result.Action, Is.Null);
            That(mockLogger.Invocations, Is.Empty);
        });
    }

    [Test]
    public void ShouldReturnVariationForNonBandit()
    {
        var mockLogger = new Mock<IAssignmentLogger>();
        mockLogger.Setup(mock => mock.LogAssignment(It.IsAny<AssignmentLogData>()));
        var client = CreateClient(mockLogger.Object);

        var result = client.GetBanditAction("non_bandit_flag", _subject, new Dictionary<string, ContextAttributes>(), "defaultValue");

        Multiple(() =>
        {
            That(result, Is.Not.Null);
            That(result.Variation, Is.EqualTo("control"));
            That(result.Action, Is.Null);
            mockLogger.VerifyAll();
        });
    }

    [Test, TestCaseSource(nameof(GetTestAssignmentData))]
    public void ShouldAssignCorrectlyAgainstUniversalTestCases(BanditTestCase banditTestCase)
    {
        var client = CreateClient();

        That(banditTestCase.Subjects, Is.Not.Empty);

        foreach (var subject in banditTestCase.Subjects)
        {
            var expected = subject.Assignment;
            Dictionary<string, ContextAttributes> actions =
                subject.Actions.ToDictionary(
                    atr => atr.ActionKey,
                    atr => ContextAttributes.FromNullableAttributes(atr.ActionKey, atr.CategoricalAttributes, atr.NumericAttributes));


            var result = client.GetBanditAction(
                banditTestCase.Flag,
                subject.SubjectKey,
                subject.SubjectAttributes.AsDict(),
                subject.Actions.ToDictionary(atr => atr.ActionKey, atr => atr.AsDict()),
                banditTestCase.DefaultValue
            );
            Multiple(() =>
            {
                That(result.Variation, Is.EqualTo(expected.Variation), "Unexpected assignment in " + banditTestCase.TestCaseFile);
                That(result.Action, Is.EqualTo(expected.Action), "Unexpected assignment in " + banditTestCase.TestCaseFile);
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

        if (testCases.Count == 0)
        {
            throw new Exception("Danger! Danger! No Test Cases Loaded. Do not proceed until solved");
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
                               Dictionary<string, double?> NumericAttributes)
{
    public IDictionary<string, object?> AsDict()
    {
        var combinedDictionary = new Dictionary<string, object?>();

        foreach (var kvp in NumericAttributes)
        {
            combinedDictionary.Add(kvp.Key, kvp.Value);
        }

        foreach (var kvp in CategoricalAttributes)
        {
            combinedDictionary.Add(kvp.Key, kvp.Value);
        }
        return combinedDictionary;
    }
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
