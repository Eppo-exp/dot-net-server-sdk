using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.helpers;
using eppo_sdk.http;
using eppo_sdk.store;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test.store;

public class ConfigurationStoreTest
{

    private ConfigurationStore? Store;
    MemoryCache? ConfigCache;
    MemoryCache? ModleCache;
    MemoryCache? BanditFlagCache;

    BanditFlags BanditFlags;
    

    [SetUp]
    public void TestSetUp() {
        ConfigCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        ModleCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        BanditFlagCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        BanditFlags = new BanditFlags();
        var response = new FlagConfigurationResponse()
        {
            Bandits = BanditFlags,
            Flags = new Dictionary<string, Flag>()
        };
        var banditResponse = new BanditModelResponse()
        {
            Bandits = new Dictionary<string, Bandit>()
        };

        var mockRequester = new Mock<IConfigurationRequester>();
        mockRequester.Setup(m => m.FetchFlagConfiguration()).Returns(response);
        mockRequester.Setup(m => m.FetchBanditModels()).Returns(banditResponse);

        Store = new ConfigurationStore(mockRequester.Object, ConfigCache, ModleCache, BanditFlagCache);
    }
    [Test]
    public void ShouldStoreAndGetBanditFlags()
    {

        Store!.FetchConfiguration();

        Assert.That(Store.GetBanditFlags(), Is.EqualTo(BanditFlags));
    }

    [Test]
    public void ShouldClearCacheOnFetch()
    {
        // MemoryCache.Clear is non-overridable so we can't mock it and verify the method call
        // Instead, we populate the caches and ensure they're empty when they should be.
        ConfigCache!.Set<string>("foo", "bar", new MemoryCacheEntryOptions().SetSize(1));
        ModleCache!.Set<string>("foo", "bar", new MemoryCacheEntryOptions().SetSize(1));
        BanditFlagCache!.Set<string>("foo", "bar", new MemoryCacheEntryOptions().SetSize(1));

        Multiple(() =>
        {
            That(ConfigCache, Has.Count.EqualTo(1));
            That(ModleCache, Has.Count.EqualTo(1));
            That(BanditFlagCache, Has.Count.EqualTo(1));
        });

        Store!.FetchConfiguration();

        Multiple(() =>
        {
            That(ConfigCache, Has.Count.EqualTo(0));
            That(ModleCache, Has.Count.EqualTo(0));
            That(BanditFlagCache, Has.Count.EqualTo(1));
            That(BanditFlagCache.Get<BanditFlags>("bandit_flags"), Is.EqualTo(BanditFlags));
        });
    }
}
