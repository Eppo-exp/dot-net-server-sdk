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
        EppoClient.Init(config, startPolling: true);
    }

    private void SetupMockServer()
    {
        _mockServer = WireMockServer.Start();
        var response = GetMockFlagConfig();
        Console.WriteLine($"MockServer started at: {_mockServer.Urls[0]}");
        this._mockServer
            .Given(Request.Create().UsingGet().WithPath(new RegexMatcher("flag-config.*")))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody(response).WithHeader("Content-Type", "application/json"));
    }

    [OneTimeTearDown]
    public void TearDown()
    {
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
        var client = EppoClient.GetInstance();
        

        switch (assignmentTestCase.VariationType)
        {
            case (EppoValueType.BOOLEAN):
                var boolExpectations = assignmentTestCase.Subjects.ConvertAll(x => (bool?)x.Assignment);
                var assignments = assignmentTestCase.Subjects.ConvertAll(subject =>
                    client.GetBoolAssignment(assignmentTestCase.Flag, subject.SubjectKey, subject.SubjectAttributes));

                Assert.That(assignments, Is.EqualTo(boolExpectations));

                break;
            case (EppoValueType.INTEGER):
                var longExpectations = assignmentTestCase.Subjects.ConvertAll(x => (long?)x.Assignment);
                var longAssignments = assignmentTestCase.Subjects.ConvertAll(subject =>
                    client.GetIntegerAssignment(assignmentTestCase.Flag, subject.SubjectKey, subject.SubjectAttributes));

                Assert.That(longAssignments, Is.EqualTo(longExpectations));

                break;

            case (EppoValueType.JSON):
                // var longExpectations = assignmentTestCase.Subjects.ConvertAll(x => (long?)x.Assignment);
                // var longAssignments = assignmentTestCase.Subjects.ConvertAll(subject =>
                //     client.GetIntegerAssignment(assignmentTestCase.Flag, subject.SubjectKey, subject.SubjectAttributes));

                // Assert.That(longAssignments, Is.EqualTo(longExpectations));

                break;
            // case (EppoValueType.NUMERIC):
            //     var numExpectation = assignmentTestCase.Subjects.ConvertAll(x => (double?)x.Assignment);
            //     var numAssignments = assignmentTestCase.Subjects.ConvertAll(subject =>
            //         client.GetNumericAssignment(assignmentTestCase.Flag, subject.SubjectKey, subject.SubjectAttributes));

            //     Assert.That(numAssignments, Is.EqualTo(numExpectation));

            //     break;
            // case "number":
            //     var numericExpectations = assignmentTestCase.expectedAssignments.ConvertAll(x => (double?)x);
            //     Assert.That(GetNumericAssignments(assignmentTestCase), Is.EqualTo(numericExpectations));

            //     break;
            // case "integer":
            //     var intExpectations = assignmentTestCase.expectedAssignments.ConvertAll(x => (long?)x);
            //     Assert.That(GetIntegerAssignments(assignmentTestCase), Is.EqualTo(intExpectations));

            //     break;
            // case "string":
            //     var stringExpectations = assignmentTestCase.expectedAssignments.ConvertAll(x => (string?)x);
            //     Assert.That(GetStringAssignments(assignmentTestCase), Is.EqualTo(stringExpectations));

            //     break;
        }
        Console.WriteLine(assignmentTestCase);
    }


    // private static List<double?> GetNumericAssignments(AssignmentTestCase assignmentTestCase)
    // {
    //     var client = EppoClient.GetInstance();
    //     if (assignmentTestCase.subjectsWithAttributes != null)
    //     {
    //         return assignmentTestCase.subjectsWithAttributes.ConvertAll(subject => client.GetNumericAssignment(subject.subjectKey, assignmentTestCase.experiment,
    //             subject.subjectAttributes));
    //     }

    //     return assignmentTestCase.subjects.ConvertAll(subject =>
    //         client.GetNumericAssignment(subject, assignmentTestCase.experiment));
    // }

    // private static List<long?> GetIntegerAssignments(AssignmentTestCase assignmentTestCase)
    // {
    //     var client = EppoClient.GetInstance();
    //     if (assignmentTestCase.subjectsWithAttributes != null)
    //     {
    //         return assignmentTestCase.subjectsWithAttributes.ConvertAll(subject => client.GetIntegerAssignment(subject.subjectKey, assignmentTestCase.experiment,
    //             subject.subjectAttributes));
    //     }

    //     return assignmentTestCase.subjects.ConvertAll(subject =>
    //         client.GetIntegerAssignment(subject, assignmentTestCase.experiment));
    // }

    // private static List<string?> GetStringAssignments(AssignmentTestCase assignmentTestCase)
    // {
    //     var client = EppoClient.GetInstance();
    //     if (assignmentTestCase.subjectsWithAttributes != null)
    //     {
    //         return assignmentTestCase.subjectsWithAttributes.ConvertAll(subject => client.GetStringAssignment(subject.subjectKey, assignmentTestCase.experiment,
    //             subject.subjectAttributes));
    //     }

    //     return assignmentTestCase.subjects.ConvertAll(subject =>
    //         client.GetStringAssignment(subject, assignmentTestCase.experiment));
    // }

    private static List<AssignmentTestCase?> GetTestAssignmentData()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory).Parent?.Parent?.Parent?.FullName;
        return Directory.EnumerateFiles($"{dir}/files/ufc/tests", "*.json")
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


public class AssignmentTestCase
{
    public required string Flag { get; set; }
    public EppoValueType VariationType { get; set; } = EppoValueType.STRING;
    public required object DefaultValue;

    public required List<SubjectTestRecord> Subjects { get; set; }

}

public record SubjectTestRecord(string SubjectKey, Subject SubjectAttributes, object Assignment);
