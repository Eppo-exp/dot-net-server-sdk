namespace eppo_sdk.dto.bandit;

public class DoubleDictionary: Dictionary<string, double>{}

public class StringDictionary : Dictionary<string, string>{};

public record AttributeSet(DoubleDictionary NumericAttributes, StringDictionary CategoricalAttributes);
