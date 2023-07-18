using System.Runtime.InteropServices.JavaScript;

namespace eppo_sdk.dto;

public class AssignmentLogData
{
    public string experiment;
    public string variation;
    public DateTime timestamp;
    public string subject;
    public SubjectAttributes subjectAttributes;

    public AssignmentLogData(string experiment, string variation, string subject, SubjectAttributes subjectAttributes) {
        this.experiment = experiment;
        this.variation = variation;
        this.timestamp = new DateTime();
        this.subject = subject;
        this.subjectAttributes = subjectAttributes;
    }
}
