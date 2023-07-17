namespace dot_net_eppo.dto;

public record ExperimentConfiguration
{
    public string name { get; set; }
    public bool enabled { get; set; }
    public int subjectShards { get; set; }
    public Dictionary<string, EppoValue> overrides { get; set; }
    public Dictionary<string, Allocation> allocations { get; set; }
    public List<Rule> rules { get; set; }
}