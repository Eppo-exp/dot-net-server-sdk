using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.exception;
using eppo_sdk.http;
using Microsoft.Extensions.Caching.Memory;

namespace eppo_sdk.store;

public class ConfigurationStore : IConfigurationStore
{
    private readonly MemoryCache _experimentConfigurationCache;
    private readonly MemoryCache _banditModelCache;
    private readonly ConfigurationRequester _requester;
    private static Dictionary<string, ConfigurationStore> _instance = new();

    public ConfigurationStore(ConfigurationRequester requester, MemoryCache flagConfigurationCache, MemoryCache banditModelCache)
    {
        _requester = requester;
        _experimentConfigurationCache = flagConfigurationCache;
        _banditModelCache = banditModelCache;
    }


    /// Gets an instance of `ConfigurationStore`
    /// Instances are indexed by the `UID` of the `ConfigurationRequester` which is based on the underlying URL
    /// Mutliple instances, hashed by URL are supported to allow parrallel testing of the EppoClient.
    public static ConfigurationStore GetInstance(MemoryCache flagConfigurationCache,
                                                 MemoryCache banditModelCache,
                                                 ConfigurationRequester requester)
    {
        // if (_instance == null)
        if (!_instance.TryGetValue(requester.UID, out ConfigurationStore? value) || value == null)
        {
            _instance[requester.UID] = new ConfigurationStore(requester, flagConfigurationCache, banditModelCache) ;
        }
        else
        {
            value._experimentConfigurationCache.Clear();
        }

        return _instance[requester.UID];
    }

    public void SetExperimentConfiguration(string key, Flag experimentConfiguration)
    {
        _experimentConfigurationCache.Set(key, experimentConfiguration, new MemoryCacheEntryOptions().SetSize(1));
    }

    public void SetBanditModel(Bandit banditModel)
    {
        _banditModelCache.Set(banditModel.BanditKey, banditModel, new MemoryCacheEntryOptions().SetSize(1));
    }

    public Flag? GetExperimentConfiguration(string key)
    {
        try
        {
            if (_experimentConfigurationCache.TryGetValue(key, out Flag? result))
            {
                return result;
            }
        }
        catch (Exception)
        {
            throw new ExperimentConfigurationNotFound($"[Eppo SDK] Experiment configuration for key: {key} not found.");
        }

        return null;
    }


    public Bandit? GetBanditModel(string key)
    {
        if (_banditModelCache.TryGetValue(key, out Bandit? result))
        {
            return result;
        }

        return null;
    }

    public void FetchConfiguration()
    {
        FlagConfigurationResponse experimentConfigurationResponse = Get();
        experimentConfigurationResponse.Flags.ToList()
            .ForEach(x => { this.SetExperimentConfiguration(x.Key, x.Value); });

        BanditModelResponse banditModels = GetBandits();
        banditModels.Bandits?.ToList()
            .ForEach(x => { this.SetBanditModel(x.Value); });
    }

    private FlagConfigurationResponse Get()
    {
        FlagConfigurationResponse? response = this._requester.FetchFlagConfiguration();
        if (response != null)
        {
            return response;
        }

        throw new SystemException("Unable to fetch experiment configuration");
    }
    private BanditModelResponse GetBandits()
    {
        BanditModelResponse? response = this._requester.FetchBanditModels();
        if (response != null)
        {
            return response;
        }

        throw new SystemException("Unable to fetch bandit models");
    }
}
