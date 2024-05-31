using System.Globalization;
using System.Numerics;
using eppo_sdk.exception;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace eppo_sdk.dto;


public class HasEppoValue
{

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private bool _typed;

    private Object? _value;
    public Object? Value
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
    public EppoValueType Type { get { return _type; } }

    private T _nonNullValue<T>(Func<object, T> func)
    {
        if (_value == null)
        {
            throw new UnsupportedEppoValueException($"Value of type {Type} is null or invalid");
        }
        return func(_value);
    }
    public bool BoolValue() => _nonNullValue<bool>((o) => (bool)o);
    public double DoubleValue() => _nonNullValue(Convert.ToDouble);
    public long IntegerValue() => _nonNullValue<long>((o) => (long)o);

    public string StringValue() => _nonNullValue<string>((o) => (string)o);
    public List<string> ArrayValue() => _nonNullValue<List<string>>((object o) =>
        {
            if (o is JArray array)
            {
                return new List<string>(array.ToObject<string[]>());
            }
            return (List<string>)_value;
        });

    public JObject JsonValue() => _nonNullValue<JObject>((o) => (JObject)o);

    private static EppoValueType InferTypeFromValue(Object? value)
    {
        if (value == null) return EppoValueType.NULL;

        if (value is Array || value.GetType().IsArray || value is JArray || value is List<string> || value is IEnumerable<string>)
        {
            return EppoValueType.ARRAY_OF_STRING;
        }
        else if (value is bool || value is Boolean)
        {
            return EppoValueType.BOOLEAN;
        }
        else if (value is float || value is double || value is Double || value is float)
        {
            return EppoValueType.NUMBER;

        }
        else if (value is int || value is long || value is BigInteger)
        {
            return EppoValueType.INTEGER;

        }
        else if (value is string || value is String)
        {
            return EppoValueType.STRING;
        }
        else if (value is JObject)
        {
            return EppoValueType.JSON;
        }
        else
        {
            Type type = value!.GetType();
            Logger.Error($"Unexpected value of type {type}");
            Console.WriteLine($"Unexpected value of type {type}");
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
        this.Value = value;
    }

    [JsonConstructor]
    public HasEppoValue(object? value)
    {
        this.Value = value;
    }

    public HasEppoValue(List<string> array)
    {
        this.Value = array;
    }

    public bool IsNumeric() => _type == EppoValueType.NUMBER || _type == EppoValueType.INTEGER;

    public bool IsNull() => EppoValueType.NULL.Equals(Type);

    public static bool IsNullValue(HasEppoValue? value) =>
     value == null /* null pointer */ ||
         value.IsNull() /* parsed as a null JSON token type */ ||
         value.Value == null; /* Value type is set but value is null */

}
