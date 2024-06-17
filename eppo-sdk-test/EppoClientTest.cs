using System.Net;
using eppo_sdk;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace eppo_sdk_test;

[TestFixture]
public class EppoClientTest
{
    private WireMockServer? _mockServer;
    private EppoClient? _client;

    [OneTimeSetUp]
    public void Setup()
    {
        SetupMockServer();
        var config = new EppoClientConfig("mock-api-key", new TestAssignmentLogger())
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
        // EppoClient.DeInit();
        _mockServer?.Stop();
    }

    private static string GetMockFlagConfig()
    {
        var filePath = Path.Combine(new DirectoryInfo(Environment.CurrentDirectory).Parent?.Parent?.Parent?.FullName,
            "files/ufc/flags-v1.json");
        using var sr = new StreamReader(filePath);
        return sr.ReadToEnd();
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
        var testCases = new List<AssignmentTestCase>(){};
        foreach (var file in files) {
            var atc = JsonConvert.DeserializeObject<AssignmentTestCase>(File.ReadAllText(file))!;
            atc.TestCaseFile = file;
            testCases.Add(atc);
        }
        return testCases;
    }
}

internal class TestAssignmentLogger : IAssignmentLogger
{
    public void LogAssignment(AssignmentLogData assignmentLogData)
    {
        // Do nothing
    }

    public void LogBanditAction(BanditLogEvent banditLogEvent)
    {
        // Do nothing
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
