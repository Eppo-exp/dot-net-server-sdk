using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace eppo_sdk.dto;

public class Condition
{

    public string Attribute { get; set; }

    public OperatorType Operator { get; set; }
    public EppoValue Value { get; set; }

    public override string ToString()
    {
        return $"Operator: {Operator} | Attribute: {Attribute} | Value: {Value}";
    }
}