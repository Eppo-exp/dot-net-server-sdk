using System.Diagnostics.CodeAnalysis;

namespace eppo_sdk.dto.bandit;

public record BanditEvaluation
{

    [SetsRequiredMembers]
    public BanditEvaluation(string flagKey,
                            string subjectKey,
                            AttributeSet subjectAttributes,
                            string selectedAction,
                            AttributeSet? actionAttributes,
                            double actionScore,
                            double actionWeight,
                            double gamma,
                            double optimalityGap)
    {
        FlagKey = flagKey;
        SubjectKey = subjectKey;
        SubjectAttributes = subjectAttributes;
        ActionKey = selectedAction;
        ActionAttributes = actionAttributes;
        ActionScore = actionScore;
        ActionWeight = actionWeight;
        Gamma = gamma;
        OptimalityGap = optimalityGap;
    }

    public required string FlagKey { get; init; }
    public required string SubjectKey { get; init; }
    public required AttributeSet SubjectAttributes { get; init; }
    public string ActionKey { get; init; }
    public AttributeSet? ActionAttributes { get; init; }
    public double ActionScore { get; init; }
    public double ActionWeight { get; init; }
    public double Gamma { get; init; }
    public double OptimalityGap { get; init; }
}
