using System.Collections.Generic;

namespace eppo_sdk.dto;

public record Split(
    string VariationKey,
    List<Shard> Shards,
    IReadOnlyDictionary<string, object>? ExtraLogging
) { }
