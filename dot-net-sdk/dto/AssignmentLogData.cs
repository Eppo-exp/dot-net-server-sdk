namespace eppo_sdk.dto;

public class AssignmentLogData
{
    public string experiment;
    public string feature_flag;
    public string allocation;
    public string variation;
    public DateTime timestamp;
    public string subject;
    public SubjectAttributes subjectAttributes;

    public AssignmentLogData(
        string feature_flag,
        string allocation,
        string variation,
        string subject,
        SubjectAttributes subjectAttributes)
    {
        this.experiment = feature_flag + "-" + allocation;
        this.feature_flag = feature_flag;
        this.allocation = allocation;
        this.variation = variation;
        this.timestamp = new DateTime();
        this.subject = subject;
        this.subjectAttributes = subjectAttributes;
    }
}
