namespace eppo_sdk.dto;

public record BanditReference(string ModelVersion,
                              BanditFlagVariation[] FlagVariations);
