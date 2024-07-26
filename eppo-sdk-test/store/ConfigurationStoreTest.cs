using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.helpers;
using eppo_sdk.http;
using eppo_sdk.store;
using Moq;
using static NUnit.Framework.Assert;


namespace eppo_sdk_test.store;

public class ConfigurationStoreTest
{

    private ConfigurationStore CreateConfigurationStore(IConfigurationRequester requester)
    {
        var configCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        var modelCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        var metadataCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;

        return new ConfigurationStore(requester, configCache, modelCache, metadataCache);
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
        mockRequester.Setup(m => m.FetchFlagConfiguration(It.IsAny<string>())).Returns(
            new VersionedResource<FlagConfigurationResponse>(response));
        mockRequester.Setup(m => m.FetchBanditModels()).Returns(new VersionedResource<BanditModelResponse>(banditResponse));

        var store = CreateConfigurationStore(mockRequester.Object);
        store.LoadConfiguration();

        Assert.That(store.GetBanditFlags(), Is.EqualTo(banditFlags));
    }

    [Test]
    public void ShouldSendIfNonMatchHeaderWithLastEtag()
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
        mockRequester.Setup(m => m.FetchFlagConfiguration(It.IsAny<string>())).Returns(new VersionedResource<FlagConfigurationResponse>(response, "ETAG"));
        mockRequester.Setup(m => m.FetchBanditModels()).Returns(new VersionedResource<BanditModelResponse>(banditResponse));

        var store = CreateConfigurationStore(mockRequester.Object);
        store.LoadConfiguration();
        mockRequester.Verify(m => m.FetchFlagConfiguration(null), Times.Exactly(1));

        // ETAG should be set for second call
        store.LoadConfiguration();
        mockRequester.Verify(m => m.FetchFlagConfiguration("ETAG"), Times.Exactly(1));
    }

    [Test]
    public void ShouldNotSetConfigIfNotModified()
    {
        var configCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        var modelCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        var metadataCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;

        var banditFlags = new BanditFlags();
        var response = new FlagConfigurationResponse()
        {
            Bandits = banditFlags,
            Flags = new Dictionary<string, Flag>()
            {
                ["flag1"] = new Flag("flag1", true, new List<Allocation>(), EppoValueType.STRING, new Dictionary<string, Variation>(), 10_000)
            }
        };
        var banditResponse = new BanditModelResponse()
        {
            Bandits = new Dictionary<string, Bandit>()
        };

        var mockRequester = new Mock<IConfigurationRequester>();
        mockRequester.Setup(m => m.FetchFlagConfiguration(It.IsAny<string>())).Returns(new VersionedResource<FlagConfigurationResponse>(response, "ETAG", isModified: false));

        var store = new ConfigurationStore(mockRequester.Object, configCache, modelCache, metadataCache);

        store.LoadConfiguration();
        Assert.Multiple(() =>
        {
            Assert.That(configCache, Is.Empty);
            Assert.That(store.TryGetFlag("flag1", out Flag? flag), Is.False);
            Assert.That(flag, Is.Null);
        });
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
        mockRequester.Setup(m => m.FetchFlagConfiguration(It.IsAny<string>())).Returns(new VersionedResource<FlagConfigurationResponse>(response));
        mockRequester.Setup(m => m.FetchBanditModels()).Returns(new VersionedResource<BanditModelResponse>(banditResponse));

        var store = CreateConfigurationStore(mockRequester.Object);
        store.LoadConfiguration();

        Assert.That(store.GetBanditFlags().Keys, Is.EquivalentTo(new List<string> { "unchangingBandit", "departingBandit" }));

        // Now, reload the config with new BanditFlags.
        mockRequester.Setup(m => m.FetchFlagConfiguration(It.IsAny<string>())).Returns(
            new VersionedResource<FlagConfigurationResponse>(
                new FlagConfigurationResponse()
                {
                    Bandits = banditFlags2,
                    Flags = new Dictionary<string, Flag>()
                }));

        store.LoadConfiguration();

        Assert.That(store.GetBanditFlags().Keys, Is.EquivalentTo(new List<string> { "unchangingBandit", "newBandit" }));
    }

    [Test]
    public void ShouldClearOldValuesOnSet()
    {
        var store = CreateConfigurationStore(new Mock<IConfigurationRequester>().Object);

        var flag1 = new Flag("flag1", true, new(), EppoValueType.NUMERIC, new(), 10000);
        var flag2 = new Flag("flag2", true, new(), EppoValueType.NUMERIC, new(), 10000);
        var flag3 = new Flag("flag3", true, new(), EppoValueType.NUMERIC, new(), 10000);

        var flags1 = new Flag[] {
            flag1,flag2
         };

        var flags2 = new Flag[]
        {
            flag1, flag3
        };

        store.SetConfiguration(flags1, null, null);

        AssertHasFlag(store, "flag1");
        AssertHasFlag(store, "flag2");
        AssertHasFlag(store, "flag3", false);

        store.SetConfiguration(flags2, null, null);

        AssertHasFlag(store, "flag1");
        AssertHasFlag(store, "flag3");
        AssertHasFlag(store, "flag2", false);

        store.SetConfiguration(Array.Empty<Flag>(), null, null);

        AssertHasFlag(store, "flag1", false);
        AssertHasFlag(store, "flag2", false);
        AssertHasFlag(store, "flag3", false);
    }

    private static void AssertHasFlag(ConfigurationStore store, string flagKey, bool hasFlag = true)
    {
        if (hasFlag)
        {
            Multiple(() =>
            {
                That(store.TryGetFlag(flagKey, out Flag? flag), Is.True);
                That(flag, Is.Not.Null);
                That(flag!.key, Is.EqualTo(flagKey));
            });
        }
        else
        {
            Multiple(() =>
            {
                That(store.TryGetFlag(flagKey, out Flag? flag), Is.False);
                That(flag, Is.Null);
            });
        }
    }
}
