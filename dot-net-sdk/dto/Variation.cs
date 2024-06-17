using System.Diagnostics.CodeAnalysis;

namespace eppo_sdk.dto;

public class Variation : HasEppoValue
{
    public required string Key {get; set;}

    [SetsRequiredMembers]
    public Variation(string key, object value) {
        Key = key;
        Value = value;
    }
}
