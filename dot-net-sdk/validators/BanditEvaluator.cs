using System.Collections.Specialized;
using eppo_sdk.dto.bandit;
using eppo_sdk.exception;
using eppo_sdk.helpers;

namespace eppo_sdk.validators;


/// Scores and selects and action based on the supplied contexts and Bandit Model data.
public class BanditEvaluator
{

    private readonly int totalShards;

    public BanditEvaluator(int totalShards = 10_000)
    {
        this.totalShards = totalShards;
    }

    public BanditEvaluation EvaluateBandit(
        string flagKey,
        ContextAttributes subject,
        IDictionary<string, ContextAttributes> actionsWithContexts,
        ModelData banditModel)
    {
        if (actionsWithContexts.Count == 0)
        {
            throw new ArgumentException("No actions provided for bandit evaluation");
        }

        // Score all potential actions.
        var actionScores = ScoreActions(
            subject.AsAttributeSet(),
            actionsWithContexts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.AsAttributeSet()),
            banditModel);

        // Assign action weights using FALCON.
        var actionWeights = WeighActions(
            actionScores,
            banditModel.Gamma,
            banditModel.ActionProbabilityFloor);

        // Shuffle the actions and select one based on the subject's bucket.
        var selectedAction = SelectAction(
            flagKey,
            subject.Key,
            actionWeights);

        var selectedActionContext = actionsWithContexts[selectedAction];
        var actionScore = actionScores[selectedAction];
        var actionWeight = actionWeights[selectedAction];

        // Determine optimality gap
        var max = actionScores.Max(score => score.Value);
        var gap = max - actionScore;

        return new BanditEvaluation(
            flagKey,
            subject.Key,
            subject.AsAttributeSet(),
            selectedAction,
            selectedActionContext.AsAttributeSet(),
            actionScore,
            actionWeight,
            banditModel.Gamma,
            gap
        );
    }

    public static IDictionary<string, double> ScoreActions(AttributeSet subjectAttributes,
                                                 IDictionary<string, AttributeSet> actionsWithContexts,
                                                 ModelData banditModel) =>
        actionsWithContexts.ToDictionary(
            kvp => kvp.Key,
            kvp => banditModel.Coefficients.TryGetValue(kvp.Key, out var coefficients)
                    ? ScoreAction(subjectAttributes, kvp.Value, coefficients)
                    : banditModel.DefaultActionScore);


    private static double ScoreAction(AttributeSet subjectAttributes,
                                      AttributeSet actionAttributes,
                                      ActionCoefficients coefficients)
    {
        double score = coefficients.Intercept;

        score += ScoreNumericAttributes(
            coefficients.SubjectNumericCoefficients,
            subjectAttributes.NumericAttributes);
        score += ScoreCategoricalAttributes(
            coefficients.SubjectCategoricalCoefficients,
            subjectAttributes.CategoricalAttributes);
        score += ScoreNumericAttributes(
            coefficients.ActionNumericCoefficients,
            actionAttributes.NumericAttributes);
        score += ScoreCategoricalAttributes(
            coefficients.ActionCategoricalCoefficients,
            actionAttributes.CategoricalAttributes);

        return score;
    }

    public static IDictionary<string, double> WeighActions(IDictionary<string, double> actionScores,
                                                    double gamma,
                                                    double probabilityFloor)
    {
        var numberOfActions = actionScores.Count;

        // Order by key then by value to get the highest score, tie broken by action key.
        var bestAction = actionScores.ToList().OrderByDescending(action=>action.Value).ThenBy(action=>action.Key).First();

        var minProbability = probabilityFloor / numberOfActions;

        var weights = actionScores
            .Where(t => t.Key != bestAction.Key)
            .ToDictionary(
                kvp => kvp.Key,
                t => Math.Max(minProbability, 1.0f / (numberOfActions + gamma * (bestAction.Value - t.Value))));


        var remainingWeight = Math.Max(0.0, 1.0 - weights.Sum(w => w.Value));
        weights[bestAction.Key] = remainingWeight;

        return weights;
    }

    private string SelectAction(string flagKey,
                                string subjectKey,
                                IDictionary<string, double> actionWeights)
    {
        // Shuffle the actions "randomly" by using the sharder to hash and bucket them
        var sortedActionWeights = actionWeights.OrderBy(t => Sharder.GetShard($"{flagKey}-{subjectKey}-{t.Key}", totalShards))
            .ThenBy(t => t.Key) // tie-breaker using action name
            .ToList();

        var shard = Sharder.GetShard($"{flagKey}-{subjectKey}", totalShards);
        var cumulativeWeight = 0.0;
        var shardValue = shard / (double)totalShards;

        for (int idx = 0; idx < sortedActionWeights.Count; idx++)
        {
            var (actionKey, weight) = sortedActionWeights[idx];
            cumulativeWeight += weight;
            if (cumulativeWeight > shardValue)
            {
                return actionKey;
            }
        }

        // Mathematically speaking, this shouldn't happen so long as the rest of the algortihm runs correctly.
        throw new BanditEvaluationException($"[Eppo SDK] No action selected for {flagKey} {subjectKey}");
    }

    public static double ScoreNumericAttributes(IReadOnlyList<NumericAttributeCoefficient> coefficients,
                                                IDictionary<string, double> attributes)
    {
        double score = 0.0f;
        foreach (var coefficient in coefficients)
        {
            if (attributes.TryGetValue(coefficient.AttributeKey, out var value))
            {
                score += coefficient.Coefficient * value;
            }
            else
            {
                score += coefficient.MissingValueCoefficient;
            }
        }
        return score;
    }

    public static double ScoreCategoricalAttributes(IReadOnlyList<CategoricalAttributeCoefficient> coefficients,
                                                    IDictionary<string, string> attributes)
    {
        double score = 0.0f;
        foreach (var coefficient in coefficients)
        {
            if (attributes.TryGetValue(coefficient.AttributeKey, out var value) && coefficient.ValueCoefficients.TryGetValue(value, out var coeff))
            {
                score += coeff;
            }
            else
            {
                score += coefficient.MissingValueCoefficient;
            }
        }
        return score;
    }
}
