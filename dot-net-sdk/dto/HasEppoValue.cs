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

    private Object? _value;
    public Object? Value
    {
        get { return _value; }
        set
        {
            _type = InferTypeFromValue(value, out object? typedValue);
            _value = typedValue ?? value;
        }
    }
    private EppoValueType _type;
    public EppoValueType Type
    {
        get { return _type; }
    }

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

    public long IntegerValue() => _nonNullValue<long>(Convert.ToInt64);

    public string StringValue() => _nonNullValue<string>((o) => o.ToString() ?? "");

    public List<string> ArrayValue() =>
        _nonNullValue<List<string>>(
            (object o) =>
            {
                if (o is JArray array)
                {
                    return new List<string>(array.ToObject<string[]>() ?? Array.Empty<string>());
                }
                return (List<string>)o;
            }
        );

    public JObject JsonValue() => _nonNullValue<JObject>((o) => (JObject)o);

    private static EppoValueType InferTypeFromValue(Object? value, out Object? typedValue)
    {
        typedValue = null;
        if (value == null)
            return EppoValueType.NULL;

        if (
            value is Array
            || value.GetType().IsArray
            || value is JArray
            || value is List<string>
            || value is IEnumerable<string>
        )
        {
            return EppoValueType.ARRAY_OF_STRING;
        }
        else if (value is bool || value is Boolean)
        {
            return EppoValueType.BOOLEAN;
        }
        else if (value is float || value is double || value is Double || value is float)
        {
            return EppoValueType.NUMERIC;
        }
        else if (value is int || value is long || value is BigInteger)
        {
            return EppoValueType.INTEGER;
        }
        else if (value is string || value is String)
        {
            // This string could be encoded JSON.
            if (TryGetJObject((string)value, out var jObject))
            {
                typedValue = jObject;
                return EppoValueType.JSON;
            }
            return EppoValueType.STRING;
        }
        else if (value is JObject)
        {
            return EppoValueType.JSON;
        }
        else
        {
            Type type = value!.GetType();
            Logger.Error($"[Eppo SDK] Unexpected value of type {type}");
            return EppoValueType.NULL;
        }
    }

    private static bool TryGetJObject(string jsonString, out JObject? jObject)
    {
        jObject = null;
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return false;
        }

        try
        {
            // Attempt to parse the JSON string using JToken.Parse
            var token = JToken.Parse(jsonString);
            // Check if the parsed token is of type JObject (represents an object)
            if (token is JObject @object)
            {
                jObject = @object;
                return true;
            }
            return false;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }

    public static HasEppoValue Bool(string? value) => new(value, EppoValueType.BOOLEAN);

    public static HasEppoValue Bool(bool value) => new(value, EppoValueType.BOOLEAN);

    public static HasEppoValue Number(string value) => new(value, EppoValueType.NUMERIC);

    public static HasEppoValue String(string? value) => new(value, EppoValueType.STRING);

    public static HasEppoValue Integer(string value) => new(value, EppoValueType.INTEGER);

    public static HasEppoValue Null() => new();

    public HasEppoValue() { }

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

    public bool IsNumeric() => _type == EppoValueType.NUMERIC || _type == EppoValueType.INTEGER;

    public bool IsNull() => EppoValueType.NULL.Equals(Type);

    public static bool IsNullValue(HasEppoValue? value) =>
        value == null /* null pointer */
        || value.IsNull() /* parsed as a null JSON token type */
        || value.Value == null; /* Value type is set but value is null */
}
