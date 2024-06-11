using System.Diagnostics.CodeAnalysis;

namespace eppo_sdk.dto.bandit;

public record BanditEvaluation{

    [SetsRequiredMembers]
    public BanditEvaluation(string flagKey,
                            string subjectKey,
                            IDictionary<string, object> subjectAttributes,
                            string selectedAction,
                            IDictionary<string, object> actionAttributes,
                            double actionScore,
                            double actionWeight,
                            double gamma)
    {
        FlagKey = flagKey;
        SubjectKey = subjectKey;
        SubjectAttributes = subjectAttributes;
        ActionKey = selectedAction;
        ActionAttributes = actionAttributes;
        ActionScore = actionScore;
        ActionWeight = actionWeight;
        Gamma = gamma;
    }

    public required string FlagKey { get; init; }
    public required string SubjectKey { get; init; }
    public required IDictionary<string, object> SubjectAttributes { get; init; }
    public string ActionKey { get; init; }
    public IDictionary<string, object>? ActionAttributes { get; init; }
    public double ActionScore { get; init; }
    public double ActionWeight { get; init; }
    public double Gamma { get; init; }
}