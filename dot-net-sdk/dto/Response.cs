using eppo_sdk.dto.bandit;

namespace eppo_sdk.dto;

public class FlagConfigurationResponse
{
    public required Dictionary<string, Flag> Flags { get; set; }
    public BanditFlags? Bandits { get; set; }
}
public class BanditModelResponse
{
    public required Dictionary<string, Bandit> Bandits { get; set; }
}
