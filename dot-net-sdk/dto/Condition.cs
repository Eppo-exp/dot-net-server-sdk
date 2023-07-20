using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace eppo_sdk.dto;

public class Condition
{
    public string attribute { get; set; }
    public EppoValue value { get; set; }

    [JsonProperty(PropertyName = "operator", NamingStrategyType = typeof(DefaultNamingStrategy))]
    public OperatorType operatorType { get; set; }

    public override string ToString()
    {
        return $"operator: {operatorType} | Attribute: {attribute} | value: {value}";
    }
}