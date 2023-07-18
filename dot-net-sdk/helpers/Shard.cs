using System.Globalization;
using eppo_sdk.dto;

namespace eppo_sdk.helpers;

public class Shard
{
    public static string GetHex(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes);
    }

    public static int GetShard(string input, int maxShardValue)
    {
        string hashText = Shard.GetHex(input);
        return (int)long.Parse(hashText.Substring(0, 8), NumberStyles.HexNumber) % maxShardValue;
    }

    public static bool IsInRange(int shard, ShardRange range)
    {
        return shard >= range.start && shard < range.end;
    }
}