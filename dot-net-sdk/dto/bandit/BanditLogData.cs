using System.Runtime.Serialization;

namespace eppo_sdk.dto.bandit;

public record BanditLogEvent(
    string FlagKey,
    string BanditKey,
    string SubjectKey,
    string? Action,
    double? ActionProbability,
    double? OptimalityGap,
    string? ModelVersion,
    DateTime Timestamp,
    IReadOnlyDictionary<string, double>? SubjectNumericAttributes,
    IReadOnlyDictionary<string, string>? SubjectCategoricalAttributes,
    IReadOnlyDictionary<string, double>? ActionNumericAttributes,
    IReadOnlyDictionary<string, string>? ActionCategoricalAttributes,
    IReadOnlyDictionary<string, string> MetaData
) : ISerializable
{
    public BanditLogEvent(
        string variation,
        BanditEvaluation result,
        Bandit model,
        IReadOnlyDictionary<string, string> metaData
    )
        : this(
            result.FlagKey,
            variation,
            result.SubjectKey,
            result.ActionKey,
            result.ActionScore,
            result.OptimalityGap,
            model.ModelVersion,
            DateTime.Now,
            result.SubjectAttributes.NumericAttributes.AsReadOnly(),
            result.SubjectAttributes.CategoricalAttributes.AsReadOnly(),
            result.ActionAttributes?.NumericAttributes.AsReadOnly(),
            result.ActionAttributes?.CategoricalAttributes.AsReadOnly(),
            metaData
        ) { }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(FlagKey), FlagKey);
        info.AddValue(nameof(BanditKey), BanditKey);
        info.AddValue(nameof(SubjectKey), SubjectKey);
        info.AddValue(nameof(Action), Action);
        info.AddValue(nameof(ActionProbability), ActionProbability);
        info.AddValue(nameof(OptimalityGap), OptimalityGap);
        info.AddValue(nameof(ModelVersion), ModelVersion);
        info.AddValue(nameof(Timestamp), Timestamp);
        info.AddValue(
            nameof(SubjectNumericAttributes),
            SubjectNumericAttributes,
            typeof(IReadOnlyDictionary<string, double>)
        );
        info.AddValue(
            nameof(SubjectCategoricalAttributes),
            SubjectCategoricalAttributes,
            typeof(IReadOnlyDictionary<string, string>)
        );
        info.AddValue(
            nameof(ActionNumericAttributes),
            ActionNumericAttributes,
            typeof(IReadOnlyDictionary<string, double>)
        );
        info.AddValue(
            nameof(ActionCategoricalAttributes),
            ActionCategoricalAttributes,
            typeof(IReadOnlyDictionary<string, string>)
        );
        info.AddValue(nameof(MetaData), MetaData, typeof(IReadOnlyDictionary<string, string>));
    }
}
