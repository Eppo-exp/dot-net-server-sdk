namespace eppo_sdk.dto;

public class ShardRange
{
    public int start { get; set; }
    public int end { get; set; }

    public override string ToString()
    {
        return $"[start: {start} | end: {end}]";
    }
}