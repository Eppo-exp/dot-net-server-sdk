using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace eppo_sdk.dto;

public record Condition(string Attribute, OperatorType Operator, EppoValue Value)
{
    public override string ToString() => $"Operator: {Operator} | Attribute: {Attribute} | Value: {Value}";
}