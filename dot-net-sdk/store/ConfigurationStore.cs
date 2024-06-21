using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.exception;
using eppo_sdk.http;
using Microsoft.Extensions.Caching.Memory;

namespace eppo_sdk.store;

public class ConfigurationStore : IConfigurationStore
{
    private readonly MemoryCache _experimentConfigurationCache;
    private readonly MemoryCache _banditFlagCache;
    private readonly MemoryCache _banditModelCache;
    private readonly IConfigurationRequester _requester;
    private static ConfigurationStore? _instance;
    private const string BANDIT_FLAGS_KEY = "bandit_flags";

    public ConfigurationStore(IConfigurationRequester requester,
                              MemoryCache flagConfigurationCache,
                              MemoryCache banditModelCache,
                              MemoryCache banditFlagCache)
    {
        _requester = requester;
        _experimentConfigurationCache = flagConfigurationCache;
        _banditModelCache = banditModelCache;
        _banditFlagCache = banditFlagCache;
    }

    public static ConfigurationStore GetInstance(IConfigurationRequester requester,
                                                 MemoryCache flagConfigurationCache,
                                                 MemoryCache banditModelCache,
                                                 MemoryCache banditFlagCache)
    {
        if (_instance == null)
        {
            _instance = new ConfigurationStore(
                requester,
                flagConfigurationCache,
                banditModelCache,
                banditFlagCache);
        }
        else
        {
            _instance._experimentConfigurationCache.Clear();
        }

        return _instance;
    }

    public void SetExperimentConfiguration(string key, Flag experimentConfiguration)
    {
        _experimentConfigurationCache.Set(key, experimentConfiguration, new MemoryCacheEntryOptions().SetSize(1));
    }

    public void SetBanditModel(Bandit banditModel)
    {
        _banditModelCache.Set(banditModel.BanditKey, banditModel, new MemoryCacheEntryOptions().SetSize(1));
    }

    public void SetBanditFlags(BanditFlags banditFlags)
    {
        _banditFlagCache.Set(BANDIT_FLAGS_KEY, banditFlags, new MemoryCacheEntryOptions().SetSize(1));
    }

    public BanditFlags GetBanditFlags()
    {
        if (_banditFlagCache.TryGetValue(BANDIT_FLAGS_KEY, out BanditFlags? banditFlags) && banditFlags != null)
        {
            return banditFlags;
        }
        throw new SystemException("Bandit Flag mapping could not be loaded from the cache");
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
        _experimentConfigurationCache.Clear();
        _banditModelCache.Clear();
        _flagBanditCache.Clear();
        
        FlagConfigurationResponse experimentConfigurationResponse = Get();
        experimentConfigurationResponse.Flags.ToList()
            .ForEach(x => { this.SetExperimentConfiguration(x.Key, x.Value); });

        if (experimentConfigurationResponse.Bandits != null)
        {
            this.SetBanditFlags(experimentConfigurationResponse.Bandits);
        }

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