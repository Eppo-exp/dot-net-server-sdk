namespace eppo_sdk.dto.bandit;

public record BanditLogEvent(string FlagKay,
                             string BanditKey,
                             string SubjectKey,
                             string? Action,
                             double? ActionProbability,
                             double? OptimalityGap,
                             string? ModelVersion,
                             DateTime Timestamp,
                             IReadOnlyDictionary<string, double>? subjectNumericAttributes,
                             IReadOnlyDictionary<string, string>? subjectCategoricalAttributes,
                             IReadOnlyDictionary<string, double>? actionNumericAttributes,
                             IReadOnlyDictionary<string, string>? actionCategoricalAttributes,
                             IReadOnlyDictionary<string, string> metaData)
{

    public BanditLogEvent(
        string variation,
        BanditEvaluation result,
        Bandit model,
        IReadOnlyDictionary<string, string> metaData) : this(
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
    )
    { }
}
