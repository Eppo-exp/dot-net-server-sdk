namespace eppo_sdk.dto;

public record Allocation(string key, List<Rule> rules, List<Split> splits, bool doLog, long? startAt, long? endAt)
{
}
