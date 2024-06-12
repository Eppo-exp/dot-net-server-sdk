namespace eppo_sdk.dto.bandit;

using DoubleDictionary = Dictionary<string, double>;
using StringDictionary = Dictionary<string, string>;
public record AttributeSet(DoubleDictionary NumericAttributes, StringDictionary CategoricalAttributes);
