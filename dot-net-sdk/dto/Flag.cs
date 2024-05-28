namespace eppo_sdk.dto;

public record Flag(string key, bool enabled, List<Allocation> allocations, EppoValueType variationType, List<Variation> variations, int totalShards)
{
}