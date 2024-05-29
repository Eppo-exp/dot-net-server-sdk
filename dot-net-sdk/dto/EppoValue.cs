using System.Globalization;
using Newtonsoft.Json;

namespace eppo_sdk.dto;

[JsonConverter(typeof(EppoValueDeserializer))]
public class EppoValue
{
    public string? value { get; set; }

    public EppoValueType type { get; set; } = EppoValueType.NULL;

    public List<string>? array { get; set; }


    public static EppoValue Bool(string? value) => new(value, EppoValueType.BOOLEAN);
    public static EppoValue Bool(bool value) => new(value.ToString(), EppoValueType.BOOLEAN);
    public static EppoValue Number(string value) => new(value, EppoValueType.NUMBER);
    public static EppoValue String(string? value) => new(value, EppoValueType.STRING);
    public static EppoValue Integer(string value) => new(value, EppoValueType.INTEGER);
    public static EppoValue Null() => new();

    public EppoValue()
    {
    }

    public EppoValue(string? value, EppoValueType type)
    {
        this.value = value;
        this.type = type;
    }

    public EppoValue(List<string> array)
    {
        this.array = array;
        this.type = EppoValueType.ARRAY_OF_STRING;
    }

    public EppoValue(EppoValueType type) => this.type = type;

    public bool BoolValue() => bool.Parse(value);

    public double DoubleValue() => double.Parse(value, NumberStyles.Number);

    public int IntegerValue() => int.Parse(value, NumberStyles.Number);

    public bool IsNumeric() => double.TryParse(value, out _);

    public string StringValue() => value;

    public List<string> ArrayValue() => array;

    public bool IsNull() => EppoValueType.NULL.Equals(type);

    public static bool IsNullValue(EppoValue? value) =>
     value == null /* null pointer */ ||
         value.IsNull() /* parsed as a null JSON token type */ ||
         value.value == null; /* Value type is set but value is null */

}
