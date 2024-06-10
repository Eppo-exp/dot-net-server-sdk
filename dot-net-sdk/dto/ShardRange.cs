namespace eppo_sdk.dto;

public record ShardRange(int start, int end)
{
    public override string ToString()
    {
        return $"[start: {start} | end: {end}]";
    }
}
