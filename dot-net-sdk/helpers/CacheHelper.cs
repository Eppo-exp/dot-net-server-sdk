using dot_net_eppo.dto;

namespace dot_net_sdk.helpers;

public class CacheHelper
{
    private CacheManager _CacheManager;

    public CacheHelper()
    {
        this._CacheManager = CacheManagerBuilder.build();
        this._CacheManager.Init();
    }

    public Cache<string, ExperimentConfiguration> CreateExperimentConfiguration(int maxEntries)
    {
        throw new NotImplementedException();
    }
}