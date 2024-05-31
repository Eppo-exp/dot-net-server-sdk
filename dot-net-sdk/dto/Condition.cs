using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace eppo_sdk.dto;

public class Condition : HasEppoValue
{
    public string Attribute { get; set; }

    public OperatorType Operator { get; set; }

    public override string ToString()
    {
        return $"operator: {Operator} | Attribute: {Attribute} | value: {Value}";
    }
}
