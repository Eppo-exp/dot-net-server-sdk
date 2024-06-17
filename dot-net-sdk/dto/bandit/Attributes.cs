using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace eppo_sdk.dto.bandit;

public record AttributeSet
{
    [JsonProperty("numeric_attributes")]
    public IDictionary<string, double> NumericAttributes {get;}
    [JsonProperty("categorical_attributes")]
    public IDictionary<string, string> CategoricalAttributes {get;}

    public AttributeSet(IDictionary<string, double> numericAttributes, IDictionary<string, string> categoricalAttributes)
    {
        NumericAttributes = numericAttributes;
        CategoricalAttributes = categoricalAttributes;
    }
}
