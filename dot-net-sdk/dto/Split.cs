using System.Collections.Generic;
namespace eppo_sdk.dto;

public class Split
{
    public string variationKey { get; }
    public List<Shard> shards { get; set; }
    public IReadOnlyDictionary<string, object> extraLogging { get; }

    public Split(string variationKey, IEnumerable<Shard> shards, IDictionary<string, object> extraLogging)
    {
        this.variationKey = variationKey;
        this.shards = new List<Shard>(shards);
        this.extraLogging = extraLogging != null ? new Dictionary<string, object>( extraLogging).AsReadOnly() : new Dictionary<string, object>();
    }
}
