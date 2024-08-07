using eppo_sdk.dto.bandit;

namespace eppo_sdk.dto;

public class FlagConfigurationResponse
{
    public required Dictionary<string, Flag> Flags { get; set; }
    public BanditReferences? BanditReferences { init; get; }
}
public class BanditModelResponse
{
    public required Dictionary<string, Bandit> Bandits { get; set; }
}
