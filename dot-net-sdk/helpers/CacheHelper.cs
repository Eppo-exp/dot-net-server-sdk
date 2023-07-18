using Microsoft.Extensions.Caching.Memory;

namespace eppo_sdk.helpers;

public class CacheHelper
{
    public MemoryCache Cache { get; private set; }

    public CacheHelper(int maxEntries)
    {
        this.Cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = maxEntries
        });
    }
}