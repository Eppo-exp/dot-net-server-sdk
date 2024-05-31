namespace eppo_sdk.dto;

public class Variation : HasEppoValue
{
    public string Key {get; set;}
    public ShardRange shardRange { get; set; }
}