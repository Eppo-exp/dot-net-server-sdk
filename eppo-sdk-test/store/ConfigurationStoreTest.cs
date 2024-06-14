using eppo_sdk.constants;
using eppo_sdk.helpers;
using eppo_sdk.http;
using eppo_sdk.store;

namespace eppo_sdk_test.store;

[TestFixture]
public class ConfigurationStoreTest
{
    [Test]
    public void ShouldAllowMultipleIndexInstances() {
        
            var configCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
            var modelCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;

        var ht1 = new EppoHttpClient("","","","http://url1");
        var cr1 = new ConfigurationRequester(ht1);

        var ht2 = new EppoHttpClient("","","","http://url2");
        var cr2 = new ConfigurationRequester(ht2);

        var ht3 = new EppoHttpClient("","","","http://url1");
        var cr3 = new ConfigurationRequester(ht3);

        var store1 = ConfigurationStore.GetInstance(configCache, modelCache, cr1);
        var store2 = ConfigurationStore.GetInstance(configCache, modelCache, cr2);
        var store3 = ConfigurationStore.GetInstance(configCache, modelCache, cr3);
        

        Assert.That(store1, Is.Not.EqualTo(store2));
        Assert.That(store1, Is.EqualTo(store3));
    }
}
