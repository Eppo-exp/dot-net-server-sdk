using System.Reflection;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using Microsoft.Extensions.Caching.Memory;

namespace eppo_sdk.store;

public class ConfigurationStore : IConfigurationStore
{
    private readonly MemoryCache ufcCache;
    private readonly MemoryCache metadataCache;
    private readonly MemoryCache banditCache;

    private readonly MemoryCacheEntryOptions cacheOptions = new MemoryCacheEntryOptions().SetSize(
        1
    );

    /// <summary>
    /// Used for concurrent-read and exclusive-write locking.
    /// </summary>
    private readonly ReaderWriterLockSlim cacheLock = new();

    public ConfigurationStore(
        MemoryCache flagConfigurationCache,
        MemoryCache banditModelCache,
        MemoryCache metadataCache
    )
    {
        this.ufcCache = flagConfigurationCache;
        this.banditCache = banditModelCache;
        this.metadataCache = metadataCache;
    }

    public Configuration GetConfiguration()
    {
        cacheLock.EnterReadLock();
        try
        {
            var flags = ExtractFromCache<Flag>(ufcCache);
            var bandits = ExtractFromCache<Bandit>(banditCache);
            var metadata = ExtractFromCache<object>(metadataCache);

            return new Configuration(flags.Values, bandits.Values, metadata);
        }
        finally
        {
            cacheLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Extracts all entries from a MemoryCache using reflection.
    /// This is necessary because MemoryCache doesn't provide enumeration capabilities.
    /// </summary>
    private static Dictionary<string, T> ExtractFromCache<T>(MemoryCache cache)
    {
        var result = new Dictionary<string, T>();

        try
        {
            // Use reflection to access the private _store field
            var field = typeof(MemoryCache).GetField(
                "_store",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (field?.GetValue(cache) is IDictionary<object, object> store)
            {
                foreach (var entry in store.Values)
                {
                    // Access the Key and Value properties of each cache entry
                    var keyProperty = entry.GetType().GetProperty("Key");
                    var valueProperty = entry.GetType().GetProperty("Value");

                    if (
                        keyProperty?.GetValue(entry) is string key
                        && valueProperty?.GetValue(entry) is T value
                    )
                    {
                        result[key] = value;
                    }
                }
            }
        }
        catch (Exception)
        {
            // If reflection fails, return empty dictionary
            // This ensures the system remains stable even if internal implementation changes
        }

        return result;
    }

    public void SetConfiguration(IEnumerable<Flag> flags, IDictionary<string, object> metadata)
    {
        cacheLock.EnterWriteLock();
        try
        {
            SetFlagsInner(flags);
            SetMetadataInner(metadata);
        }
        finally
        {
            cacheLock.ExitWriteLock();
        }
    }

    public void SetConfiguration(
        IEnumerable<Flag> flags,
        IEnumerable<Bandit> bandits,
        IDictionary<string, object> metadata
    )
    {
        cacheLock.EnterWriteLock();
        try
        {
            SetFlagsInner(flags);
            SetMetadataInner(metadata);
            SetBanditsInner(bandits);
        }
        finally
        {
            cacheLock.ExitWriteLock();
        }
    }

    private void SetBanditsInner(IEnumerable<Bandit> bandits)
    {
        banditCache.Clear();
        foreach (var bandit in bandits)
        {
            banditCache.Set(bandit.BanditKey, bandit, cacheOptions);
        }
    }

    private void SetMetadataInner(IDictionary<string, object> metadata)
    {
        metadataCache.Clear();
        foreach (KeyValuePair<string, object> kvp in metadata)
        {
            metadataCache.Set(kvp.Key, kvp.Value, cacheOptions);
        }
    }

    private void SetFlagsInner(IEnumerable<Flag> flags)
    {
        ufcCache.Clear();
        foreach (var flag in flags)
        {
            ufcCache.Set(flag.Key, flag, cacheOptions);
        }
    }

    public bool TryGetFlag(string key, out Flag? result)
    {
        cacheLock.EnterReadLock();
        try
        {
            return ufcCache.TryGetValue(key, out result);
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
            return banditCache.TryGetValue(key, out bandit);
        }
        finally
        {
            cacheLock.ExitReadLock();
        }
    }

    public bool TryGetMetadata<MType>(string key, out MType? metadata)
    {
        cacheLock.EnterReadLock();
        try
        {
            return metadataCache.TryGetValue(key, out metadata);
        }
        finally
        {
            cacheLock.ExitReadLock();
        }
    }
}
