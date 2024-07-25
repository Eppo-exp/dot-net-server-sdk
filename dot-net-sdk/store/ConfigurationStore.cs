using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.exception;
using eppo_sdk.http;
using Microsoft.Extensions.Caching.Memory;

namespace eppo_sdk.store;

public class ConfigurationStore : IConfigurationStore
{
    private readonly MemoryCache _flagConfigurationCache;
    private readonly MemoryCache _metadataCache;
    private readonly MemoryCache _banditModelCache;
    private readonly IConfigurationRequester _requester;
    private const string BANDIT_FLAGS_KEY = "bandit_flags";
    private const string FLAG_RESOURCE_ETAG = "ufc_etag";

    private static readonly object Baton = new();
    private readonly ReaderWriterLockSlim cacheLock = new();

    public ConfigurationStore(IConfigurationRequester requester,
                              MemoryCache flagConfigurationCache,
                              MemoryCache banditModelCache,
                              MemoryCache metadataCache)
    {
        _requester = requester;
        _flagConfigurationCache = flagConfigurationCache;
        _banditModelCache = banditModelCache;
        _metadataCache = metadataCache;
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
        _metadataCache.Set(BANDIT_FLAGS_KEY, banditFlags, new MemoryCacheEntryOptions().SetSize(1));
    }

    public BanditFlags GetBanditFlags()
    {
        cacheLock.EnterReadLock();
        try
        {
            if (_metadataCache.TryGetValue(BANDIT_FLAGS_KEY, out BanditFlags? banditFlags) && banditFlags != null)
            {
                return banditFlags;
            }
        }
        finally
        {
            cacheLock.ExitReadLock();
        }
        throw new SystemException("Bandit Flag mapping could not be loaded from the cache");
    }


    public bool TryGetFlag(string key, out Flag? result)
    {
        cacheLock.EnterReadLock();
        try
        {
            return _flagConfigurationCache.TryGetValue(key, out result);
        }
        finally
        {
            cacheLock.ExitReadLock();
        }
    }

    public bool TryGetBandit(string key, out Bandit? bandit)
    {
        cacheLock.EnterReadLock();
        try
        {
            return _banditModelCache.TryGetValue(key, out bandit);
        }
        finally
        {
            cacheLock.ExitReadLock();
        }
    }

    private void ClearCaches()
    {
        _flagConfigurationCache.Clear();
        _banditModelCache.Clear();
        _metadataCache.Clear();
    }
    public void LoadConfiguration()
    {
        // Get the last tag for flags.
        string? etag = GetLastFlagVersion();


        var flagConfigurationResponse = FetchFlags(etag);
        if (!flagConfigurationResponse.IsModified)
        {
            return;
        }

        var flags = flagConfigurationResponse.Resource;
        var banditModels = FetchBandits().Resource;

        SetConfiguration(
            flags.Flags.ToList().Select(kvp => kvp.Value),
            flags.Bandits,
            banditModels.Bandits?.ToList().Select(kvp => kvp.Value),
            flagConfigurationResponse.ETag);
    }

    private string? GetLastFlagVersion()
    {
        cacheLock.EnterReadLock();
        var eTag = _metadataCache.Get<string>(FLAG_RESOURCE_ETAG);
        cacheLock.ExitReadLock();
        return eTag;
    }

    public void SetConfiguration(IEnumerable<Flag> flags, BanditFlags? banditFlags, IEnumerable<Bandit>? bandits, string? eTag = null)
    {
        cacheLock.EnterWriteLock();
        try
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
            _metadataCache.Set(FLAG_RESOURCE_ETAG, eTag, new MemoryCacheEntryOptions().SetSize(1));
        }
        finally
        {
            cacheLock.ExitWriteLock();
        }
    }

    private VersionedResource<FlagConfigurationResponse> FetchFlags(string? lastEtag = null)
    {
        var response = _requester.FetchFlagConfiguration(lastEtag);
        if (response != null)
        {
            return response;
        }

        throw new SystemException("Unable to fetch flag configuration");
    }
    private VersionedResource<BanditModelResponse> FetchBandits()
    {
        var response = _requester.FetchBanditModels();
        if (response != null)
        {
            return response;
        }

        throw new SystemException("Unable to fetch bandit models");
    }
}
