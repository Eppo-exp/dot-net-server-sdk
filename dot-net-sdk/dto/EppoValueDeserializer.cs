using eppo_sdk.exception;
using Newtonsoft.Json;

namespace eppo_sdk.dto;

public class EppoValueDeserializer : JsonConverter<EppoValue>
{
    public override void WriteJson(JsonWriter writer, EppoValue? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override EppoValue? ReadJson(JsonReader reader, Type objectType, EppoValue? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var value = reader.Value;
        switch (reader.TokenType)
        {
            case JsonToken.String:
                return EppoValue.String(value.ToString());
            case JsonToken.Integer:
                return EppoValue.Integer(value.ToString());
            case JsonToken.Float:
                return EppoValue.Number(value.ToString());
            case JsonToken.Boolean:
                return EppoValue.Bool(value.ToString());
            case JsonToken.Null:
                return EppoValue.Null();
            case JsonToken.StartArray:
                var val = new List<string>();
                reader.Read();
                while (reader.TokenType != JsonToken.EndArray)
                {
                    val.Add(reader.Value.ToString());
                    reader.Read();
                }
                return new EppoValue(val);
            default:
                throw new UnsupportedEppoValueException("Unsupported Eppo Value Type: " + reader.TokenType.ToString());
        }
    }
}