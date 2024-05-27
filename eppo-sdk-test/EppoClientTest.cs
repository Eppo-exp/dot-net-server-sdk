using System.Net;
using eppo_sdk;
using eppo_sdk.dto;
using eppo_sdk.logger;
using Newtonsoft.Json;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace eppo_sdk_test;

public class EppoClientTest
{
    private WireMockServer? _mockServer;

    [OneTimeSetUp]
    public void Setup()
    {
        SetupMockServer();
        var config = new EppoClientConfig("mock-api-key", new TestAssignmentLogger())
        {
            BaseUrl = _mockServer?.Urls[0]!
        };
        EppoClient.Init(config);
    }

    private void SetupMockServer()
    {
        _mockServer = WireMockServer.Start();
        var response = GetMockRandomizedAssignments();
        Console.WriteLine($"MockServer started at: {_mockServer.Urls[0]}");
        this._mockServer
            .Given(Request.Create().UsingGet().WithPath(new RegexMatcher(".*randomized_assignment.*")))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody(response).WithHeader("Content-Type", "application/json"));
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _mockServer?.Stop();
    }

    private static string GetMockRandomizedAssignments()
    {
        var filePath = Path.Combine(new DirectoryInfo(Environment.CurrentDirectory).Parent?.Parent?.Parent?.FullName,
            "files/rac-experiments-v3.json");
        using var sr = new StreamReader(filePath);
        return sr.ReadToEnd();
    }

    [Test, TestCaseSource(nameof(GetTestAssignmentData))]
    public void ShouldValidateAssignments(AssignmentTestCase assignmentTestCase)
    {

        switch (assignmentTestCase.valueType)
        {
            case "boolean":
                var boolExpectations = assignmentTestCase.expectedAssignments.ConvertAll(x => x.BoolValue());
                Assert.That(GetBoolAssignments(assignmentTestCase), Is.EqualTo(boolExpectations));

                break;
            case "number":
                var numericExpectations = assignmentTestCase.expectedAssignments.ConvertAll(x => x.DoubleValue());
                Assert.That(GetNumericAssignments(assignmentTestCase), Is.EqualTo(numericExpectations));

                break;
            case "integer":
                var intExpectations = assignmentTestCase.expectedAssignments.ConvertAll(x => x.IntegerValue());
                Assert.That(GetIntegerAssignments(assignmentTestCase), Is.EqualTo(intExpectations));

                break;
            case "string":
                var stringExpectations = assignmentTestCase.expectedAssignments.ConvertAll(x => x.StringValue());
                Assert.That(GetStringAssignments(assignmentTestCase), Is.EqualTo(stringExpectations));

                break;
        }
    }

    private static List<bool?> GetBoolAssignments(AssignmentTestCase assignmentTestCase)
    {
        var client = EppoClient.GetInstance();
        if (assignmentTestCase.subjectsWithAttributes != null)
        {
            return assignmentTestCase.subjectsWithAttributes.ConvertAll(subject => client.GetBoolAssignment(subject.subjectKey, assignmentTestCase.experiment,
                subject.subjectAttributes));
        }

        return assignmentTestCase.subjects.ConvertAll(subject =>
            client.GetBoolAssignment(subject, assignmentTestCase.experiment));
    }

    private static List<double?> GetNumericAssignments(AssignmentTestCase assignmentTestCase)
    {
        var client = EppoClient.GetInstance();
        if (assignmentTestCase.subjectsWithAttributes != null)
        {
            return assignmentTestCase.subjectsWithAttributes.ConvertAll(subject => client.GetNumericAssignment(subject.subjectKey, assignmentTestCase.experiment,
                subject.subjectAttributes));
        }

        return assignmentTestCase.subjects.ConvertAll(subject =>
            client.GetNumericAssignment(subject, assignmentTestCase.experiment));
    }

    private static List<double?> GetIntegerAssignments(AssignmentTestCase assignmentTestCase)
    {
        var client = EppoClient.GetInstance();
        if (assignmentTestCase.subjectsWithAttributes != null)
        {
            return assignmentTestCase.subjectsWithAttributes.ConvertAll(subject => client.GetIntegerAssignment(subject.subjectKey, assignmentTestCase.experiment,
                subject.subjectAttributes));
        }

        return assignmentTestCase.subjects.ConvertAll(subject =>
            client.GetNumericAssignment(subject, assignmentTestCase.experiment));
    }

    private static List<string?> GetStringAssignments(AssignmentTestCase assignmentTestCase)
    {
        var client = EppoClient.GetInstance();
        if (assignmentTestCase.subjectsWithAttributes != null)
        {
            return assignmentTestCase.subjectsWithAttributes.ConvertAll(subject => client.GetStringAssignment(subject.subjectKey, assignmentTestCase.experiment,
                subject.subjectAttributes));
        }

        return assignmentTestCase.subjects.ConvertAll(subject =>
            client.GetStringAssignment(subject, assignmentTestCase.experiment));
    }

    private static List<AssignmentTestCase?> GetTestAssignmentData()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory).Parent?.Parent?.Parent?.FullName;
        return Directory.EnumerateFiles($"{dir}/files/assignment-v2", "*.json")
            .Select(File.ReadAllText)
            .Select(JsonConvert.DeserializeObject<AssignmentTestCase>).ToList();
    }
}

internal class TestAssignmentLogger : IAssignmentLogger
{
    public void LogAssignment(AssignmentLogData assignmentLogData)
    {
        // Do nothing
    }
}

public class SubjectWithAttributes
{
    public string subjectKey { get; set; }

    public SubjectAttributes subjectAttributes { get; set; }
}

public class AssignmentTestCase
{
    public string experiment { get; set; }
    public string valueType { get; set; } = "string";
    public List<SubjectWithAttributes>? subjectsWithAttributes { get; set; }
    public List<string> subjects { get; set; }
    public List<EppoValue> expectedAssignments { get; set; }
}