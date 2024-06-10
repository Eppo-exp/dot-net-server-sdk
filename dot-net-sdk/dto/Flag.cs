namespace eppo_sdk.dto;

public record Flag(
    string key,
    bool enabled,
    List<Allocation> Allocations,
    EppoValueType variationType,
    Dictionary<string, Variation> variations,
    int totalShards)
{
}
