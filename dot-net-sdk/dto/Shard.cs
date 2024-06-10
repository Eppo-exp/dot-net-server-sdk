namespace eppo_sdk.dto;

public record Shard(string salt, List<ShardRange> ranges)
{
}
