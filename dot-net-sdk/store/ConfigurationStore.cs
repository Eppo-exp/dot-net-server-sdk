using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.exception;
using eppo_sdk.http;
using Microsoft.Extensions.Caching.Memory;

namespace eppo_sdk.store;

public class ConfigurationStore : IConfigurationStore
{
    private readonly MemoryCache _flagConfigurationCache;
    private readonly MemoryCache _banditFlagCache;
    private readonly MemoryCache _banditModelCache;
    private readonly IConfigurationRequester _requester;
    private const string BANDIT_FLAGS_KEY = "bandit_flags";

    private static readonly object Baton = new();

    public ConfigurationStore(IConfigurationRequester requester,
                              MemoryCache flagConfigurationCache,
                              MemoryCache banditModelCache,
                              MemoryCache banditFlagCache)
    {
        _requester = requester;
        _flagConfigurationCache = flagConfigurationCache;
        _banditModelCache = banditModelCache;
        _banditFlagCache = banditFlagCache;
    }

    private void SetFlag(string key, Flag flag)
    {
        _flagConfigurationCache.Set(key, flag, new MemoryCacheEntryOptions().SetSize(1));
    }

    private void SetBanditModel(Bandit banditModel)
    {
        _banditModelCache.Set(
            banditModel.BanditKey,
            banditModel,
            new MemoryCacheEntryOptions().SetSize(1));
    }

    private void SetBanditFlags(BanditFlags banditFlags)
    {
        _banditFlagCache.Set(BANDIT_FLAGS_KEY, banditFlags, new MemoryCacheEntryOptions().SetSize(1));
    }

    public BanditFlags GetBanditFlags()
    {
        lock (Baton)
        {
            if (_banditFlagCache.TryGetValue(BANDIT_FLAGS_KEY, out BanditFlags? banditFlags) && banditFlags != null)
            {
                return banditFlags;
            }
        }
        throw new SystemException("Bandit Flag mapping could not be loaded from the cache");
    }

    public bool TryGetFlag(string key, out Flag? result)
    {
        lock (Baton)
        {
            return _flagConfigurationCache.TryGetValue(key, out result);
        }
    }

    public bool TryGetBandit(string key, out Bandit? bandit)
    {
        lock (Baton)
        {
            return _banditModelCache.TryGetValue(key, out bandit);
        }
    }

    private void ClearCaches()
    {
        _flagConfigurationCache.Clear();
        _banditModelCache.Clear();
        _banditFlagCache.Clear();
    }
    public void LoadConfiguration()
    {
        FlagConfigurationResponse flagConfigurationResponse = FetchFlags();
        BanditModelResponse banditModels = FetchBandits();
        SetConfiguration(
            flagConfigurationResponse.Flags.ToList().Select(kvp => kvp.Value),
            flagConfigurationResponse.Bandits,
            banditModels.Bandits?.ToList().Select(kvp => kvp.Value));

    }
    public void SetConfiguration(IEnumerable<Flag> flags, BanditFlags? banditFlags, IEnumerable<Bandit>? bandits)
    {
        lock (Baton)
        {
            ClearCaches();
            foreach (var flag in flags)
            {
                SetFlag(flag.key, flag);
            }
            if (banditFlags != null)
            {
                SetBanditFlags(banditFlags);
            }
            if (bandits != null)
            {
                foreach (var bandit in bandits)
                {
                    SetBanditModel(bandit);
                }
            }
        }
    }

    private FlagConfigurationResponse FetchFlags()
    {
        FlagConfigurationResponse? response = this._requester.FetchFlagConfiguration();
        if (response != null)
        {
            return response;
        }

        throw new SystemException("Unable to fetch flag configuration");
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
