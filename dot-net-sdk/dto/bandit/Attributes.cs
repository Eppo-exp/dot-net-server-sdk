using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace eppo_sdk.dto.bandit;

public class AttributeSet
{
    [JsonProperty("numeric_attributes")]
    public IDictionary<string, double> NumericAttributes { get; }
    [JsonProperty("categorical_attributes")]
    public IDictionary<string, string> CategoricalAttributes { get; }

    public IDictionary<string, object> Combined
    {
        get
        {
            var combinedDictionary = new Dictionary<string, object>();
            foreach (var a in NumericAttributes)
            {
                combinedDictionary[a.Key] = a.Value;
            }
            foreach (var a in CategoricalAttributes)
            {
                combinedDictionary[a.Key] = a.Value;
            }
            return combinedDictionary;
        }
    }

    public AttributeSet(IDictionary<string, string> categoricalAttributes, IDictionary<string, double> numericAttributes)
    {
        CategoricalAttributes = categoricalAttributes;
        NumericAttributes = numericAttributes;
    }
}
