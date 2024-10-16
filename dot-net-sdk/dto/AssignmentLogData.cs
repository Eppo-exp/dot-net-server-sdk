using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization;

namespace eppo_sdk.dto;

public record AssignmentLogData : ISerializable
{
    public string Experiment;
    public string FeatureFlag;
    public string Allocation;
    public string Variation;
    public DateTime Timestamp;
    public string Subject;
    public IReadOnlyDictionary<string, object> SubjectAttributes;

    public IReadOnlyDictionary<string, string>? ExtraLogging;
    public IReadOnlyDictionary<string, string> MetaData;

    public AssignmentLogData(string featureFlag,
                             string allocation,
                             string variation,
                             string subject,
                             IReadOnlyDictionary<string, object> subjectAttributes,
                             IReadOnlyDictionary<string, string> metaData,
                             IReadOnlyDictionary<string, string> extraLoggging

                             )
    {
        this.Experiment = featureFlag + "-" + allocation;
        this.FeatureFlag = featureFlag;
        this.Allocation = allocation;
        this.Variation = variation;
        this.Timestamp = DateTime.UtcNow;
        this.Subject = subject;
        this.SubjectAttributes = subjectAttributes;
        MetaData = metaData;
        ExtraLogging = extraLoggging;
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(Experiment), Experiment);
        info.AddValue(nameof(FeatureFlag), FeatureFlag);
        info.AddValue(nameof(Allocation), Allocation);
        info.AddValue(nameof(Variation), Variation);
        info.AddValue(nameof(Timestamp), Timestamp);
        info.AddValue(nameof(Subject), Subject);
        info.AddValue(nameof(SubjectAttributes), SubjectAttributes, typeof(IReadOnlyDictionary<string, object>));
        info.AddValue(nameof(MetaData), MetaData, typeof(IReadOnlyDictionary<string, string>));
        info.AddValue(nameof(ExtraLogging), ExtraLogging, typeof(IReadOnlyDictionary<string, string>));
    }
}
