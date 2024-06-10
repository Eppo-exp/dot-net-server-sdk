using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using eppo_sdk.dto;

namespace eppo_sdk.helpers;

public class Sharder
{
    public static string GetHex(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    public static int GetShard(string input, int maxShardValue)
    {
        var hashText = GetHex(input);
        return (int) (long.Parse(hashText[..8], NumberStyles.HexNumber) % maxShardValue);
    }

    public static bool IsInRange(int shard, ShardRange range)
    {
        return shard >= range.start && shard < range.end;
    }
}