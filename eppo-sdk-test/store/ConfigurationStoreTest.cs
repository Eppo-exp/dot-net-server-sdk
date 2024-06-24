using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.helpers;
using eppo_sdk.http;
using eppo_sdk.store;
using Moq;

namespace eppo_sdk_test.store;

public class ConfigurationStoreTest
{

    private ConfigurationStore CreateConfigurationStore(IConfigurationRequester requester)
    {
        var configCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        var modelCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        var banditFlagCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;


        return new ConfigurationStore(requester, configCache, modelCache, banditFlagCache);

    }

    [Test]
    public void ShouldStoreAndGetBanditFlags()
    {
        var banditFlags = new BanditFlags();
        var response = new FlagConfigurationResponse()
        {
            Bandits = banditFlags,
            Flags = new Dictionary<string, Flag>()
        };
        var banditResponse = new BanditModelResponse()
        {
            Bandits = new Dictionary<string, Bandit>()
        };

        var mockRequester = new Mock<IConfigurationRequester>();
        mockRequester.Setup(m => m.FetchFlagConfiguration()).Returns(response);
        mockRequester.Setup(m => m.FetchBanditModels()).Returns(banditResponse);

        var store = CreateConfigurationStore(mockRequester.Object);
        store.FetchConfiguration();

        Assert.That(store.GetBanditFlags(), Is.EqualTo(banditFlags));
    }

    [Test]
    public void ShouldResetFlagsOnLoad()
    {
        var banditFlags1 = new BanditFlags()
        {
            ["unchangingBandit"] = new BanditVariation[] { new("unchangingBandit", "flagKey", "unchangingBandit", "unchangingBandit") },
            ["departingBandit"] = new BanditVariation[] { new("departingBandit", "endingFlagKey", "departingBandit", "departingBandit") },
        };

        var banditFlags2 = new BanditFlags()
        {
            ["unchangingBandit"] = new BanditVariation[] { new("unchangingBandit", "flagKey", "unchangingBandit", "unchangingBandit") },
            ["newBandit"] = new BanditVariation[] { new("newBandit", "newBanditFlagKey", "newBandit", "newBandit") },
        };

        var response = new FlagConfigurationResponse()
        {
            Bandits = banditFlags1,
            Flags = new Dictionary<string, Flag>()
        };
        var banditResponse = new BanditModelResponse()
        {
            Bandits = new Dictionary<string, Bandit>()
        };

        var mockRequester = new Mock<IConfigurationRequester>();
        mockRequester.Setup(m => m.FetchFlagConfiguration()).Returns(response);
        mockRequester.Setup(m => m.FetchBanditModels()).Returns(banditResponse);

        var store = CreateConfigurationStore(mockRequester.Object);
        store.FetchConfiguration();

        Assert.That(store.GetBanditFlags().Keys, Is.EquivalentTo(new List<string> { "unchangingBandit", "departingBandit" }));

        // Now, reload the config with new BanditFlags.

        mockRequester.Setup(m => m.FetchFlagConfiguration()).Returns(new FlagConfigurationResponse()
        {
            Bandits = banditFlags2,
            Flags = new Dictionary<string, Flag>()
        });

        store.FetchConfiguration();

        Assert.That(store.GetBanditFlags().Keys, Is.EquivalentTo(new List<string> { "unchangingBandit", "newBandit" }));
    }
}
