namespace eppo_sdk.dto;

public record Flag(
    string Key,
    bool Enabled,
    List<Allocation> Allocations,
    EppoValueType VariationType,
    Dictionary<string, Variation> Variations,
    int TotalShards)
{
}
