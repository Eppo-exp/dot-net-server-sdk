namespace eppo_sdk.dto;

public class Rule
{
    public string allocationKey { get; set; }
    public List<Condition> conditions { get; set; }
}