namespace eppo_sdk.dto;

public class Variation : HasEppoValue
{
    public string Key { get; init; }

    public Variation(string key, object value)
    {
        Key = key;
        Value = value;
    }
}
