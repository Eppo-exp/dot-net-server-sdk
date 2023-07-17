namespace dot_net_eppo.dto;

public class Rule
{
    public string allocationKey { get; set; }
    public List<Condition> conditions { get; set; }
}