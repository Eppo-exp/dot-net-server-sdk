using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.http;
using Microsoft.Extensions.Caching.Memory;

namespace eppo_sdk.store;

public class ConfigurationStore : IConfigurationStore
{
    private readonly MemoryCache _flagConfigurationCache;
    private readonly MemoryCache _metadataCache;
    private readonly MemoryCache _banditModelCache;
    private readonly IConfigurationRequester _requester;
    private const string BANDIT_FLAGS_KEY = "banditFlags";
    private const string KEY_FLAG_CONFIG_VERSION = "ufcVersion";

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
        var flagConfigurationResponse = FetchFlags();
        if (flagConfigurationResponse.IsModified)
        {
            // Fetch methods throw if resource is null.
            var flags = flagConfigurationResponse.Resource!;
            IEnumerable<Bandit> banditModelList = Array.Empty<Bandit>();
            if (flags.Bandits?.Count > 0)
            {
                BanditModelResponse banditModels = FetchBandits().Resource!;
                banditModelList = banditModels.Bandits?.ToList().Select(kvp => kvp.Value) ?? Array.Empty<Bandit>();
            }

            SetConfiguration(
                flags.Flags.ToList().Select(kvp => kvp.Value),
                flags.Bandits,
                banditModelList,
                flagConfigurationResponse.VersionIdentifier);
        }
        else
        {
            // Write the most recent flag config version to cache.
            cacheLock.EnterWriteLock();
            _metadataCache.Set(KEY_FLAG_CONFIG_VERSION, flagConfigurationResponse.VersionIdentifier, new MemoryCacheEntryOptions().SetSize(1));
            cacheLock.ExitWriteLock();
        }
    }

    private string? GetLastFlagVersion()
    {
        cacheLock.EnterReadLock();
        var lastVersion = _metadataCache.Get<string>(KEY_FLAG_CONFIG_VERSION);
        cacheLock.ExitReadLock();
        return lastVersion;
    }

    public void SetConfiguration(IEnumerable<Flag> flags, BanditFlags? banditFlags, IEnumerable<Bandit>? bandits, string? lastVersion = null)
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
            _metadataCache.Set(KEY_FLAG_CONFIG_VERSION, lastVersion, new MemoryCacheEntryOptions().SetSize(1));
        }
        finally
        {
            cacheLock.ExitWriteLock();
        }
    }

    private VersionedResourceResponse<FlagConfigurationResponse> FetchFlags()
    {
        // The response from `ConfigurationRequester` is versioned so we can avoid extra work and network bytes
        // by keeping track of the version we have loaded.
        string? lastConfigVersion = GetLastFlagVersion();
        try
        {
            var response = _requester.FetchFlagConfiguration(lastConfigVersion);
            if (response.IsModified && response.Resource == null)
            {
                // Invalid and unexpected state.
                throw new SystemException("Flag configuration not present in response");
            }
            return response;
        }
        catch (Exception e)
        {
            throw new SystemException("Unable to fetch flag configuration" + e.Message);
        }
    }
    private VersionedResourceResponse<BanditModelResponse> FetchBandits()
    {
        try
        {
            var response = _requester.FetchBanditModels();
            if (response.Resource == null)
            {
                throw new SystemException("Bandit configuration not present in response");
            }
            return response;
        }
        catch (Exception e)
        {
            throw new SystemException("Unable to fetch bandit configuration" + e.Message);
        }
    }
}
