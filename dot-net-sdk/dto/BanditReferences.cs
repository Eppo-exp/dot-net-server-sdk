using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;

namespace eppo_sdk.dto;

public interface IBanditRefenceIndexer
{
    public bool TryGetBanditKey(string flagKey, string variationValue, out string? banditKey);
    public bool HasBandits();
    public IDictionary<string, string> GetBanditModelVersions();
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

    public bool HasBandits()
    {
        return this.Any(BanditHasVariations);
    }

    public IDictionary<string, string> GetBanditModelVersions()
    {
        return this.Where(BanditHasVariations).ToDictionary(
            (KeyValuePair<string, BanditReference> kvp) => kvp.Key,
            (KeyValuePair<string, BanditReference> kvp) => kvp.Value.ModelVersion
        );
    }

    private static bool BanditHasVariations(
        KeyValuePair<string, BanditReference> keyAndBanditReference)
             => keyAndBanditReference.Value.FlagVariations.Length > 0;
}