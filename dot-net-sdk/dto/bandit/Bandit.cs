namespace eppo_sdk.dto.bandit;

public record Bandit(string BanditKey, string ModelName, DateTime UpdatedAt, string ModelVersion, ModelData ModelData);
