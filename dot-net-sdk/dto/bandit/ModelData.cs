namespace eppo_sdk.dto.bandit;

public class ModelData
{
    public double Gamma;
    public required IDictionary<string, ActionCoefficients> Coefficients;

    public double DefaultActionScore { get; init; } = 0.0;
    public double ActionProbabilityFloor { get; init; } = 0.0;
}
