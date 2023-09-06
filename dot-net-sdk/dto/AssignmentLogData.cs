namespace eppo_sdk.dto;

public class AssignmentLogData
{
    public string experiment;
    public string featureFlag;
    public string allocation;
    public string variation;
    public DateTime timestamp;
    public string subject;
    public SubjectAttributes subjectAttributes;

    public AssignmentLogData(
        string featureFlag,
        string allocation,
        string variation,
        string subject,
        SubjectAttributes subjectAttributes)
    {
        this.experiment = featureFlag + "-" + allocation;
        this.featureFlag = featureFlag;
        this.allocation = allocation;
        this.variation = variation;
        this.timestamp = new DateTime();
        this.subject = subject;
        this.subjectAttributes = subjectAttributes;
    }
}
