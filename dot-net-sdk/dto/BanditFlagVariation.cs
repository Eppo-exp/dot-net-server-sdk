namespace eppo_sdk.dto;

public record BanditFlagVariation(string Key,
                                  string FlagKey,
                                  string AllocationKey,
                                  string VariationKey,
                                  string VariationValue);
