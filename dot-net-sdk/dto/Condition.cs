using System.Text.Json.Serialization;

namespace eppo_sdk.dto;

public class Condition
{
    public string attribute { get; set; }
    public EppoValue value { get; set; }

    [JsonPropertyName("operator")]
    public OperatorType operatorType { get; set; }

    public override string ToString()
    {
        return $"operator: {operatorType} | Attribute: {attribute} | value: {value}";
    }
}