using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace eppo_sdk.dto;

public class Condition : HasEppoValue
{
    public string Attribute { get; set; }

    public OperatorType Operator { get; set; }

    public Condition(string attribute, OperatorType op, object? value)
    {
        Attribute = attribute;
        Operator = op;
        Value = value;
    }

    public override string ToString()
    {
        return $"operator: {Operator} | Attribute: {Attribute} | value: {Value}";
    }
}
