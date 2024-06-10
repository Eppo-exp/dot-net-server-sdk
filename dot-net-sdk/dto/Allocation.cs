namespace eppo_sdk.dto;

public record Allocation(string key, List<Rule> rules, List<Split> splits, bool doLog, DateTime? startAt, DateTime? endAt)
{
}
