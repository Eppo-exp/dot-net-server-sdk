using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.exception;
using eppo_sdk.http;
using Microsoft.Extensions.Caching.Memory;

namespace eppo_sdk.store;

public class ConfigurationStore : IConfigurationStore
{
    private readonly MemoryCache _flagConfigurationCache;
    private readonly MemoryCache _banditModelCache;
    private readonly ConfigurationRequester _requester;

    public ConfigurationStore(ConfigurationRequester requester,
                              MemoryCache flagConfigurationCache,
                              MemoryCache banditModelCache)
    {
        _requester = requester;
        _flagConfigurationCache = flagConfigurationCache;
        _banditModelCache = banditModelCache;
    }

    public void SetExperimentConfiguration(string key, Flag experimentConfiguration)
    {
        _flagConfigurationCache.Set(key, experimentConfiguration, new MemoryCacheEntryOptions().SetSize(1));
    }

    public void SetBanditModel(Bandit banditModel)
    {
        _banditModelCache.Set(
            banditModel.BanditKey,
            banditModel,
            new MemoryCacheEntryOptions().SetSize(1));
    }

    public bool TryGetFlag(string key, out Flag? result) => _flagConfigurationCache.TryGetValue(key, out result);

    public bool TryGetBandit(string key, out Bandit? bandit) => _banditModelCache.TryGetValue(key, out bandit);

    public void LoadConfiguration()
    {
        FlagConfigurationResponse experimentConfigurationResponse = FetchFlags();
        experimentConfigurationResponse.Flags.ToList()
            .ForEach(x => { this.SetExperimentConfiguration(x.Key, x.Value); });

        BanditModelResponse banditModels = FetchBandits();
        banditModels.Bandits?.ToList()
            .ForEach(x => { SetBanditModel(x.Value); });
    }

    private FlagConfigurationResponse FetchFlags()
    {
        FlagConfigurationResponse? response = this._requester.FetchFlagConfiguration();
        if (response != null)
        {
            return response;
        }

        throw new SystemException("Unable to fetch experiment configuration");
    }
    private BanditModelResponse FetchBandits()
    {
        BanditModelResponse? response = this._requester.FetchBanditModels();
        if (response != null)
        {
            return response;
        }

        throw new SystemException("Unable to fetch bandit models");
    }

}
