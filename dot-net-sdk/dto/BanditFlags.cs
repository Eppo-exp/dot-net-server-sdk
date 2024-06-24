using eppo_sdk.dto.bandit;

namespace eppo_sdk.dto;

public interface IBanditFlags: IDictionary<string, BanditVariation[]> {
    public bool IsBanditFlag(string FlagKey);
}

public class BanditFlags : Dictionary<string, BanditVariation[]>, IBanditFlags
{
    public bool IsBanditFlag(string flagKey) => this.Any(kvp=> kvp.Value.Any(bv => bv.FlagKey == flagKey));
}
