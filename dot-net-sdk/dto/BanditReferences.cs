namespace eppo_sdk.dto;

public interface IBanditRefenceIndexer
{
    public bool TryGetBanditKey(string flagKey, string variationValue, out string? banditKey);
    public bool HasBanditReferences();
    public IEnumerable<string> GetBanditModelVersions();
}


public class BanditReferences : Dictionary<string, BanditReference>, IBanditRefenceIndexer
{
    public bool TryGetBanditKey(string flagKey, string variationValue, out string? banditKey)
    {
        banditKey = null;
        foreach (KeyValuePair<string, BanditReference> banditRef in this)
        {
            foreach (BanditFlagVariation bfv in banditRef.Value.FlagVariations)
            {
                if (bfv.FlagKey == flagKey && bfv.VariationValue == variationValue)
                {
                    banditKey = bfv.Key;
                    return true;
                }
            }
        }
        return false;
    }

    public bool HasBanditReferences()
    {
        return this.Any(BanditHasVariations);
    }

    public IEnumerable<string> GetBanditModelVersions()
    {
        return this.Where(BanditHasVariations).ToList().Select(
            (KeyValuePair<string, BanditReference> kvp) => kvp.Value.ModelVersion
        );
    }

    private static bool BanditHasVariations(
        KeyValuePair<string, BanditReference> keyAndBanditReference)
             => keyAndBanditReference.Value.FlagVariations.Length > 0;
}
