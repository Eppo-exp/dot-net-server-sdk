namespace dot_net_eppo.dto;

internal class ShardRange
{
    public int start;
    public int end;

    public override string ToString()
    {
        return $"[start: {start} | end: {end}]";
    }
}