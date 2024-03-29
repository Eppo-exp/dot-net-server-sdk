using System.Globalization;
using Newtonsoft.Json;

namespace eppo_sdk.dto;

[JsonConverter(typeof(EppoValueDeserializer))]
public class EppoValue
{
    public string value { get; set; }

    public EppoValueType type { get; set; } = EppoValueType.NULL;

    public List<string> array { get; set; }

    public EppoValue()
    {
    }

    public EppoValue(string value, EppoValueType type)
    {
        this.value = value;
        this.type = type;
    }

    public EppoValue(List<string> array)
    {
        this.array = array;
        this.type = EppoValueType.ARRAY_OF_STRING;
    }

    public EppoValue(EppoValueType type)
    {
        this.type = type;
    }

    public bool BoolValue()
    {
        return bool.Parse(value);
    }

    public double DoubleValue()
    {
        return double.Parse(value, NumberStyles.Number);
    }

    public bool isNumeric()
    {
        return double.TryParse(value, out _);
    }

    public string StringValue()
    {
        return value;
    }

    public List<string> ArrayValue()
    {
        return array;
    }

    public bool isNull()
    {
        return EppoValueType.NULL.Equals(type);
    }
}