using System.Collections.Generic;
namespace eppo_sdk.dto;

public record Split(string variationKey, List<Shard> shards, IReadOnlyDictionary<string, object> extraLogging)
{
}
