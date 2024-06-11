namespace eppo_sdk.dto.bandit;

public record CategoricalAttributeCoefficient(string AttributeKey, double MissingValueCoefficient, DoubleDictionary ValueCoefficients);
