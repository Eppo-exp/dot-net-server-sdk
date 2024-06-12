namespace eppo_sdk.dto.bandit;

public record AttributeSet(IDictionary<string, double> NumericAttributes, IDictionary<string, string> CategoricalAttributes);
