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

    [SetUp]
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

    [TearDown]
    public void TearDown()
    {
        _mockServer?.Stop();
    }

    private static string GetMockRandomizedAssignments()
    {
        var filePath = Path.Combine(new DirectoryInfo(Environment.CurrentDirectory).Parent?.Parent?.Parent?.FullName,
            "files/rac-experiments-v2.json");
        using var sr = new StreamReader(filePath);
        return sr.ReadToEnd();
    }

    [Test, TestCaseSource(nameof(GetTestAssignmentData))]
    public void ShouldValidateAssignments(AssignmentTestCase assignmentTestCase)
    {
        var assignments = GetAssignments(assignmentTestCase);
        Assert.That(assignments, Is.EqualTo(assignmentTestCase.expectedAssignments));
    }

    private static List<string?> GetAssignments(AssignmentTestCase assignmentTestCase)
    {
        var client = EppoClient.GetInstance();
        if (assignmentTestCase.subjectsWithAttributes != null)
        {
            return assignmentTestCase.subjectsWithAttributes.ConvertAll(subject =>
            {
                Console.WriteLine($">>>> {subject.subjectKey}");
                var assignment = client.GetAssignment(subject.subjectKey, assignmentTestCase.experiment,
                    subject.subjectAttributes);
                Console.WriteLine(assignment);
                Console.WriteLine(">>>>");
                return assignment;
            });
        }

        return assignmentTestCase.subjects.ConvertAll(subject =>
            client.GetAssignment(subject, assignmentTestCase.experiment));
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
    public List<SubjectWithAttributes>? subjectsWithAttributes { get; set; }
    public List<string> subjects { get; set; }
    public List<string> expectedAssignments { get; set; }
}