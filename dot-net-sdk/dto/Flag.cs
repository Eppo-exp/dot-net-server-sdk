namespace eppo_sdk.dto;

public record Flag(string key, bool enabled, List<Allocation> allocations, EppoValueType variationType, Dictionary<string, Variation> variations, int totalShards)
{
}
