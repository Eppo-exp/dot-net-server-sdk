namespace eppo_sdk.dto.bandit;

public record ActionCoefficients(string ActionKey, double Intercept)
{
    public IReadOnlyList<NumericAttributeCoefficient> SubjectNumericCoefficients { get; init; } =
        new List<NumericAttributeCoefficient>();
    public IReadOnlyList<CategoricalAttributeCoefficient> SubjectCategoricalCoefficients { get; init; } =
        new List<CategoricalAttributeCoefficient>();
    public IReadOnlyList<NumericAttributeCoefficient> ActionNumericCoefficients { get; init; } =
        new List<NumericAttributeCoefficient>();
    public IReadOnlyList<CategoricalAttributeCoefficient> ActionCategoricalCoefficients { get; init; } =
        new List<CategoricalAttributeCoefficient>();
}
