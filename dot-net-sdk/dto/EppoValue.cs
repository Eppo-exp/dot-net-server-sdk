using System.Dynamic;
using System.Globalization;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eppo_sdk.dto;

[JsonConverter(typeof(EppoValueDeserializer))]
public class EppoValue
{
    public string? Value { get; set; }

    public EppoValueType Type { get; set; } = EppoValueType.NULL;

    private List<string>? _array;

    private JObject? _json;


    public static EppoValue Bool(string? value) => new(value, EppoValueType.BOOLEAN);
    public static EppoValue Bool(bool value) => new(value.ToString(), EppoValueType.BOOLEAN);
    public static EppoValue Number(string value) => new(value, EppoValueType.NUMBER);
    public static EppoValue String(string? value) => new(value, EppoValueType.STRING);
    public static EppoValue Integer(string value) => new(value, EppoValueType.INTEGER);
    public static EppoValue JsonObject(string value) => new(value, EppoValueType.JSON);
    public static EppoValue Null() => new();

    public EppoValue()
    {
    }

    public EppoValue(string? value, EppoValueType type)
    {
        this.Value = value;
        this.Type = type;

        // This is helpful but parsing will actually not recognize JSON; it will merely see a string.
        if (type == EppoValueType.JSON)
        {
            _json = JObject.Parse(value);
        }
    }

    public EppoValue(List<string> array)
    {
        this._array = array;
        this.Type = EppoValueType.ARRAY_OF_STRING;
    }

    public EppoValue(EppoValueType type) => this.Type = type;

    public bool BoolValue() => bool.Parse(Value);

    public double DoubleValue() => double.Parse(Value, NumberStyles.Number);

    public int IntegerValue() => int.Parse(Value, NumberStyles.Number);

    public bool IsNumeric() => double.TryParse(Value, out _);

    public string StringValue() => Value;

    public List<string> ArrayValue() => _array;


    /**
     * Attempts to parse the value into a JObject, returns null if parsing fails.
     */
    public JObject? JsonValue()
    {
        if (_json == null && Value != null)
        {
            try
            {
                _json = JObject.Parse(Value);
            }
            catch (JsonReaderException)
            {

            }
        }
        return _json;
    }

    public bool IsNull() => EppoValueType.NULL.Equals(Type);

    public static bool IsNullValue(EppoValue? value) =>
     value == null /* null pointer */ ||
         value.IsNull() /* parsed as a null JSON token type */ ||
         value.Value == null; /* Value type is set but value is null */

}
