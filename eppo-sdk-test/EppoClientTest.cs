using System.Net;
using eppo_sdk;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.logger;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test;

[TestFixture]
public class EppoClientTest
{
    private WireMockServer? _mockServer;
    private EppoClient? _client;

    private Mock<IAssignmentLogger> _mockAssignmentLogger;

    [OneTimeSetUp]
    public void Setup()
    {
        SetupMockServer();
        _mockAssignmentLogger = new Mock<IAssignmentLogger>();
        var config = new EppoClientConfig("mock-api-key", _mockAssignmentLogger.Object)
        {
            BaseUrl = _mockServer?.Urls[0]!
        };
        _client = EppoClient.Init(config);
    }

    private void SetupMockServer()
    {
        _mockServer = WireMockServer.Start();
        var response = GetMockFlagConfig();
        Console.WriteLine($"MockServer started at: {_mockServer.Urls[0]}");
        this._mockServer
            .Given(Request.Create().UsingGet().WithPath(new RegexMatcher("flag-config/v1/config")))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody(response).WithHeader("Content-Type", "application/json"));
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _mockServer?.Stop();
    }

    [TearDown]
    public void TeardownEach()
    {
        _mockAssignmentLogger.Invocations.Clear();
    }

    private static string GetMockFlagConfig()
    {
        var filePath = Path.Combine(new DirectoryInfo(Environment.CurrentDirectory).Parent?.Parent?.Parent?.FullName,
            "files/ufc/flags-v1.json");
        using var sr = new StreamReader(filePath);
        return sr.ReadToEnd();
    }

    [Test]
    public void ShouldLogAssignment()
    {

        var alice = new Dictionary<string, object>()
        {
            ["email"] = "alice@company.com",
            ["country"] = "US"
        };
        var result = _client!.GetIntegerAssignment("integer-flag", "alice", alice, 1);

        Multiple(() =>
        {
            // Assert - Result verification
            That(result, Is.EqualTo(3));

            // Assert - Assignment logger verification
            var assignmentLogStatement = _mockAssignmentLogger.Invocations.First().Arguments[0] as AssignmentLogData;
            That(assignmentLogStatement, Is.Not.Null);
            var logEvent = assignmentLogStatement!;

            That(logEvent.FeatureFlag, Is.EqualTo("integer-flag"));
            That(logEvent.Variation, Is.EqualTo("three"));
            That(logEvent.Subject, Is.EqualTo("alice"));
        });
    }

    [Test, TestCaseSource(nameof(GetTestAssignmentData))]
    public void ShouldValidateAssignments(AssignmentTestCase assignmentTestCase)
    {
        var client = _client!;

        switch (assignmentTestCase.VariationType)
        {
            case (EppoValueType.BOOLEAN):
                var boolExpectations = assignmentTestCase.Subjects.ConvertAll(x => (bool?)x.Assignment);
                var assignments = assignmentTestCase.Subjects.ConvertAll(subject =>
                    client.GetBooleanAssignment(assignmentTestCase.Flag, subject.SubjectKey, subject.SubjectAttributes, (bool)assignmentTestCase.DefaultValue));

                Assert.That(assignments, Is.EqualTo(boolExpectations), $"Unexpected values for test file: {assignmentTestCase.TestCaseFile}");
                break;
            case (EppoValueType.INTEGER):
                var longExpectations = assignmentTestCase.Subjects.ConvertAll(x => (long?)x.Assignment);
                var longAssignments = assignmentTestCase.Subjects.ConvertAll(subject =>
                    client.GetIntegerAssignment(assignmentTestCase.Flag, subject.SubjectKey, subject.SubjectAttributes, (long)assignmentTestCase.DefaultValue));

                Assert.That(longAssignments, Is.EqualTo(longExpectations), $"Unexpected values for test file: {assignmentTestCase.TestCaseFile}");
                break;
            case (EppoValueType.JSON):
                var jsonExpectations = assignmentTestCase.Subjects.ConvertAll(x => (JObject)x.Assignment);
                var jsonAssignments = assignmentTestCase.Subjects.ConvertAll(subject =>
                    client.GetJsonAssignment(assignmentTestCase.Flag, subject.SubjectKey, subject.SubjectAttributes, (JObject)assignmentTestCase.DefaultValue));

                Assert.That(jsonAssignments, Is.EqualTo(jsonExpectations), $"Unexpected values for test file: {assignmentTestCase.TestCaseFile}");


                // Also verify that the GetJsonStringAssignment method is working.
                var jsonStringExpectations = assignmentTestCase.Subjects.ConvertAll(x => ((JObject)x.Assignment).ToString());
                var jsonStringAssignments = assignmentTestCase.Subjects.ConvertAll(subject =>
                    client.GetJsonStringAssignment(assignmentTestCase.Flag, subject.SubjectKey, subject.SubjectAttributes, ((JObject)assignmentTestCase.DefaultValue).ToString()));

                Assert.That(jsonStringAssignments, Is.EqualTo(jsonStringExpectations), $"Unexpected values for test file: {assignmentTestCase.TestCaseFile}");
                break;
            case (EppoValueType.NUMERIC):
                var numExpectations = assignmentTestCase.Subjects.ConvertAll(x => (double?)x.Assignment);
                var numAssignments = assignmentTestCase.Subjects.ConvertAll(subject =>
                    client.GetNumericAssignment(assignmentTestCase.Flag, subject.SubjectKey, subject.SubjectAttributes, (double)assignmentTestCase.DefaultValue));

                Assert.That(numAssignments, Is.EqualTo(numExpectations), $"Unexpected values for test file: {assignmentTestCase.TestCaseFile}");
                break;
            case (EppoValueType.STRING):
                var stringExpectations = assignmentTestCase.Subjects.ConvertAll(x => (string)x.Assignment);
                var stringAssignments = assignmentTestCase.Subjects.ConvertAll(subject =>
                    client.GetStringAssignment(assignmentTestCase.Flag, subject.SubjectKey, subject.SubjectAttributes, (string)assignmentTestCase.DefaultValue));

                Assert.That(stringAssignments, Is.EqualTo(stringExpectations), $"Unexpected values for test file: {assignmentTestCase.TestCaseFile}");
                break;
        }
    }


    static List<AssignmentTestCase> GetTestAssignmentData()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory).Parent?.Parent?.Parent?.FullName;
        var files = Directory.EnumerateFiles($"{dir}/files/ufc/tests", "*.json");
        var testCases = new List<AssignmentTestCase>() { };
        foreach (var file in files)
        {
            var atc = JsonConvert.DeserializeObject<AssignmentTestCase>(File.ReadAllText(file))!;
            atc.TestCaseFile = file;
            testCases.Add(atc);
        }
        if (testCases.Count == 0) {
            throw new Exception("Danger Danger. No Test Cases Loaded. Do not proceed until solved");
        }
        return testCases;
    }
}

public class AssignmentTestCase
{
    public required string Flag { get; set; }
    public EppoValueType VariationType { get; set; } = EppoValueType.STRING;
    public required object DefaultValue;
    public string? TestCaseFile;

    public required List<SubjectTestRecord> Subjects { get; set; }

}

public record SubjectTestRecord(string SubjectKey, Subject SubjectAttributes, object Assignment);
