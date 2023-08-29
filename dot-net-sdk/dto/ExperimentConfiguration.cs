namespace eppo_sdk.dto;

public class ExperimentConfiguration
{
    public string name { get; set; }
    public bool enabled { get; set; }
    public int subjectShards { get; set; }
    public Dictionary<string, EppoValue> typedOverrides { get; set; }
    public Dictionary<string, Allocation> allocations { get; set; }
    public List<Rule> rules { get; set; }

    public Allocation? GetAllocation(string allocationKey)
    {
        allocations.TryGetValue(allocationKey, out var value);
        return value;
    }
}