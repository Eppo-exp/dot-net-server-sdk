namespace eppo_sdk.dto;

public class Flag
{
    public string key { get; set; }
    public bool enabled { get; set; }
    public List<Allocation> allocations { get; set; }
    public EppoValueType variationType { get; set; }
    public List<Variation> variations { get; set; }
    public int totalShards { get; set; }

    public Flag(string key, bool enabled, IEnumerable<Allocation> allocations, EppoValueType variationType, IEnumerable<Variation> variations, int totalShards)
    {
        this.key = key;
        this.enabled = enabled;
        this.allocations = new List<Allocation>(allocations);
        this.variationType = variationType;
        this.variations = new List<Variation>(variations);
        this.totalShards = totalShards;
    }
}