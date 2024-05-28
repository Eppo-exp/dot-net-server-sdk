namespace eppo_sdk.dto;

public class ShardRange
{
    public int start { get; }
    public int end { get; }

    public ShardRange(int start, int end)
    {
        if (start > end)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "Start must be less than or equal to End.");
        }

        this.start = start;
        this.end = end;
    }


    public override string ToString()
    {
        return $"[start: {start} | end: {end}]";
    }
}
