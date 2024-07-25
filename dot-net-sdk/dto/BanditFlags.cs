using System.Collections.Specialized;
using eppo_sdk.dto.bandit;

namespace eppo_sdk.dto;

public interface IBanditFlags : IDictionary<string, BanditVariation[]>
{
    public bool IsBanditFlag(string FlagKey);

    public bool TryGetBanditKey(string FlagKey, string VariationValue, out string? BanditKey);
}

public class BanditFlags : Dictionary<string, BanditVariation[]>, IBanditFlags
{
    public bool IsBanditFlag(string flagKey) => this.Any(kvp => kvp.Value.Any(bv => bv.FlagKey == flagKey));

    public bool TryGetBanditKey(string FlagKey, string variationValue, out string? banditKey)
    {
        banditKey = null;
        try
        {
            var banditRef = this.First(kvp => kvp.Value.Any(bv => bv.FlagKey == FlagKey && bv.VariationValue == variationValue));

            var banditVariation = banditRef.Value.First(bv => bv.FlagKey == FlagKey && bv.VariationValue == variationValue);
            banditKey = banditVariation.Key;
            return true;
        }
        catch (InvalidOperationException)
        {
            // Thrown when no matching elements are found above; do nothing.
        }
        return false;
    }
}
