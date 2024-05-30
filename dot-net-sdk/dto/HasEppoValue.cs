using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eppo_sdk.dto;


public class HasEppoValue
{

    private bool _typed;

    private Object? _value;
    public Object? value
    {
        get { return _value; }
        set
        {
            if (!_typed)
            {
                _type = InferTypeFromValue(value);
                _value = value;
            }
        }
    }
    public Object? typedValue
    {
        get { return _value; }
        set
        {
            _typed = true;
            _type = InferTypeFromValue(value);
            _value = value;
        }
    }
    private EppoValueType _type;
    public EppoValueType type { get { return _type; } }

    public bool? BoolValue() => _value != null ? (bool)_value : null;
    public double? DoubleValue() => _value != null ? (double)_value : null;
    public long? IntegerValue() => _value != null ? (long)_value : null;
    public string? StringValue() => _value != null ? (string)_value : null;
    public List<string>? ArrayValue()
    {
        if (_value == null)
        {
            return null;
        }

        return new List<string>(((JArray)_value).ToObject<string[]>());

    }
    public JObject? JsonValue() => _value == null ? null : (JObject) _value;



    private static EppoValueType InferTypeFromValue(Object? value)
    {
        if (value == null) return EppoValueType.NULL;

        if (value is Array || value.GetType().IsArray || value is JArray)
        {
            return EppoValueType.ARRAY_OF_STRING;
        }
        else if (value is bool)
        {
            return EppoValueType.BOOLEAN;
        }
        else if (value is float || value is double)
        {
            return EppoValueType.NUMBER;

        }
        else if (value is int || value is long)
        {
            return EppoValueType.INTEGER;

        }
        else if (value is string)
        {
            return EppoValueType.STRING;
        }
        else if (value is JObject)
        {
            return EppoValueType.JSON;
        }
        else
        {
            var type = value!.GetType();
            Console.WriteLine($"Value {value} is of type {type}");
            return EppoValueType.NULL;
        }
    }

    public static HasEppoValue Bool(string? value) => new(value, EppoValueType.BOOLEAN);
    public static HasEppoValue Bool(bool value) => new(value, EppoValueType.BOOLEAN);
    public static HasEppoValue Number(string value) => new(value, EppoValueType.NUMBER);
    public static HasEppoValue String(string? value) => new(value, EppoValueType.STRING);
    public static HasEppoValue Integer(string value) => new(value, EppoValueType.INTEGER);
    public static HasEppoValue Null() => new();

    public HasEppoValue()
    {
    }

    public HasEppoValue(object? value, EppoValueType type)
    {
        this.value = value;
    }

    public HasEppoValue(List<string> array)
    {
        this.value = array;
    }

    public bool IsNumeric() => _type == EppoValueType.NUMBER || _type == EppoValueType.INTEGER;

    public bool IsNull() => EppoValueType.NULL.Equals(type);

    public static bool IsNullValue(HasEppoValue? value) =>
     value == null /* null pointer */ ||
         value.IsNull() /* parsed as a null JSON token type */ ||
         value.value == null; /* Value type is set but value is null */

}