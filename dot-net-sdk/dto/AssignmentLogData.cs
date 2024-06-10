using System.Reflection.Metadata.Ecma335;

namespace eppo_sdk.dto;

public record AssignmentLogData
{
    public string Experiment;
    public string FeatureFlag;
    public string Allocation;
    public string Variation;
    public DateTime Timestamp;
    public string Subject;
    public Subject SubjectAttributes;

    public IReadOnlyDictionary<string, string>? ExtraLogging;
    public IReadOnlyDictionary<string, string> MetaData;

    public AssignmentLogData(string featureFlag,
                             string allocation,
                             string variation,
                             string subject,
                             Subject subjectAttributes,
                             IReadOnlyDictionary<string, string> metaData,
                             IReadOnlyDictionary<string, string> extraLoggging
                             
                             )
    {
        this.Experiment = featureFlag + "-" + allocation;
        this.FeatureFlag = featureFlag;
        this.Allocation = allocation;
        this.Variation = variation;
        this.Timestamp = new DateTime();
        this.Subject = subject;
        this.SubjectAttributes = subjectAttributes;
        MetaData = metaData;
        ExtraLogging = extraLoggging;
    }
}
