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
                return new EppoValue(value.ToString(), EppoValueType.STRING);
            case JsonToken.Integer:
            case JsonToken.Float:
                return new EppoValue(value.ToString(), EppoValueType.NUMBER);
            case JsonToken.Boolean:
                return new EppoValue(value.ToString(), EppoValueType.BOOLEAN);
            case JsonToken.Null:
                return new EppoValue(EppoValueType.NULL);
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
                throw new UnsupportedEppoValueException("Unsupported Eppo Values");
        }
    }
}