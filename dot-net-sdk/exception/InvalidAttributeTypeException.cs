using System.Runtime.Serialization;

namespace eppo_sdk.exception;

[Serializable]
public class InvalidAttributeTypeException : Exception
{
    public string Key { get; init; }
    public object? Value { get; init; }
    public InvalidAttributeTypeException(string key, object? value) : base($"Value for {key} has invalid type {value}")
    {
        Key = key;
        Value = value;
    }
}
