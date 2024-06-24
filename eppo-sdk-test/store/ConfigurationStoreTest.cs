using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.helpers;
using eppo_sdk.http;
using eppo_sdk.store;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test.store;

public class ConfigurationStoreTest
{
    [Test]
    public void ShouldStoreAndGetBanditFlags()
    {
        var configCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        var modelCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        var banditFlagCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        var banditFlags = new BanditFlags();
        var response = new FlagConfigurationResponse() {
            Bandits = banditFlags,
            Flags = new Dictionary<string, Flag>()
        };
        var banditResponse = new BanditModelResponse() {
            Bandits = new Dictionary<string, Bandit>()
        };

        var mockRequester = new Mock<IConfigurationRequester>();
        mockRequester.Setup(m=>m.FetchFlagConfiguration()).Returns(response);
        mockRequester.Setup(m=>m.FetchBanditModels()).Returns(banditResponse);

        var store = new ConfigurationStore(mockRequester.Object, configCache, modelCache, banditFlagCache);
        store.LoadConfiguration();

        Assert.That(store.GetBanditFlags(), Is.EqualTo(banditFlags));
    }
}
