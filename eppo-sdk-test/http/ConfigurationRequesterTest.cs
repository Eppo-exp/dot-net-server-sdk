using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.helpers;
using eppo_sdk.http;
using eppo_sdk.store;
using Moq;
using NUnit.Framework.Internal;


namespace eppo_sdk_test.store;

public class ConfigurationRequesterTest
{

    private static ConfigurationStore CreateConfigurationStore()
    {
        var configCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        var modelCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        var metadataCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;

        return new ConfigurationStore(configCache, modelCache, metadataCache);
    }

    private static Flag BasicFlag(string flagKey, string[] variationValues)
    {
        var variations = variationValues.Select((v) => new Variation(v, v)).ToDictionary(v => v.Key);
        return new Flag(flagKey,
            true,
             new List<Allocation>(),
             EppoValueType.STRING, variations, 10_000);
    }

    private static Bandit BasicBandit(string banditKey, string modelVersion = "v123")
    {
        return new Bandit(banditKey, "falcon", DateTime.Now, modelVersion, new ModelData()
        {
            Coefficients = new Dictionary<string, ActionCoefficients>()
        });
    }

    private static Mock<EppoHttpClient> MockAPIWithFlagsAndBandits()
    {
        var flags = new Dictionary<string, Flag>
        {
            ["flag1"] = BasicFlag("flag1", new string[] { "control", "bandit1" })
        };
        var banditFlags = new BanditFlags()
        {
            ["bandit1"] = new BanditVariation[] {
                 new("bandit1", "flag1", "bandit1", "bandit1")
            }
        };
        var response = new FlagConfigurationResponse()
        {
            Bandits = banditFlags,
            Flags = flags
        };

        var banditResponse = new BanditModelResponse()
        {
            Bandits = new Dictionary<string, Bandit>()
            {
                ["bandit1"] = BasicBandit("bandit1")
            }
        };

        var mockAPI = new Mock<EppoHttpClient>("apiKey", "sdkName", "sdkVersion", "baseUrl");

        mockAPI.Setup(m => m.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT, It.IsAny<string>()))
            .Returns(new VersionedResourceResponse<FlagConfigurationResponse>(response, "ETAG"));
        mockAPI.Setup(m => m.Get<BanditModelResponse>(Constants.BANDIT_ENDPOINT, It.IsAny<string>()))
            .Returns(new VersionedResourceResponse<BanditModelResponse>(banditResponse, "ETAG"));

        return mockAPI;
    }

    private static Mock<EppoHttpClient> MockAPIWithFlagsOnly(string lastVersion = "lastVersion")
    {
        var flags = new Dictionary<string, Flag>
        {
            ["flag1"] = BasicFlag("flag1", new string[] { "control", "experiment" })
        };
        var response = new FlagConfigurationResponse()
        {
            Bandits = null,
            Flags = flags
        };

        var banditResponse = new BanditModelResponse()
        {
            Bandits = new Dictionary<string, Bandit>()
            {
                ["bandit1"] = BasicBandit("bandit1")
            }
        };

        var mockAPI = new Mock<EppoHttpClient>("apiKey", "sdkName", "sdkVersion", "baseUrl");

        mockAPI.Setup(m => m.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT, It.IsAny<string>()))
            .Returns(new VersionedResourceResponse<FlagConfigurationResponse>(response, lastVersion));
        mockAPI.Setup(m => m.Get<BanditModelResponse>(Constants.BANDIT_ENDPOINT, It.IsAny<string>()))
            .Returns(new VersionedResourceResponse<BanditModelResponse>(banditResponse, lastVersion));

        return mockAPI;
    }

    [Test]
    public void ShouldLoadAndStoreConfig()
    {
        var store = CreateConfigurationStore();

        var requester = new ConfigurationRequester(MockAPIWithFlagsAndBandits().Object, store);

        requester.LoadConfiguration();

        Assert.Multiple(() =>
        {
            Assert.That(requester.GetBanditFlags(), Is.Not.Null);
            Assert.That(requester.GetBanditFlags(), Has.Count.EqualTo(1));
            Assert.That(requester.GetBanditFlags().TryGetBanditKey("flag1", "bandit1", out string? banditKey), Is.True);
            Assert.That(banditKey, Is.EqualTo("bandit1"));

            Assert.That(requester.TryGetFlag("flag1", out Flag? flag), Is.True);
            Assert.That(flag, Is.Not.Null);

            Assert.That(requester.TryGetBandit("bandit1", out Bandit? bandit), Is.True);
            Assert.That(bandit, Is.Not.Null);
        });
    }

    [Test]
    public void ShouldNotLoadBanditsIfNotReferenced()
    {
        var store = CreateConfigurationStore();
        var api = MockAPIWithFlagsOnly();

        var requester = new ConfigurationRequester(api.Object, store);

        requester.LoadConfiguration();

        Assert.Multiple(() =>
        {
            Assert.That(requester.GetBanditFlags(), Is.Not.Null);
            Assert.That(requester.GetBanditFlags(), Has.Count.EqualTo(0));

            Assert.That(requester.TryGetFlag("flag1", out Flag? flag), Is.True);
            Assert.That(flag, Is.Not.Null);

            Assert.That(requester.TryGetBandit("bandit1", out Bandit? bandit), Is.False);
            Assert.That(bandit, Is.Null);
        });

        api.Verify(m => m.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT, It.IsAny<string>()), Times.Once());
        api.Verify(m => m.Get<BanditModelResponse>(Constants.BANDIT_ENDPOINT, It.IsAny<string>()), Times.Never());
    }

    [Test]
    public void ShouldSendLastVersionString()
    {
        var mockAPI = new Mock<EppoHttpClient>("apiKey", "sdkName", "sdkVersion", "baseUrl");

        var response = new FlagConfigurationResponse()
        {
            Bandits = null,
            Flags = new()
        };

        mockAPI.Setup(
            m => m.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT, It.IsAny<string>()))
            .Returns(new VersionedResourceResponse<FlagConfigurationResponse>(response, "version1"));

        var store = CreateConfigurationStore();

        var requester = new ConfigurationRequester(mockAPI.Object, store);

        requester.LoadConfiguration(); // sends null as lastversion
        requester.LoadConfiguration(); // sends version1 as lastversion

        mockAPI.Verify(m => m.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT, null), Times.Exactly(1));
        mockAPI.Verify(m => m.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT, "version1"), Times.Exactly(1));
    }

    [Test]
    public void ShouldNotSetConfigIfNotModified()
    {
        var mockAPI = new Mock<EppoHttpClient>("apiKey", "sdkName", "sdkVersion", "baseUrl");
        var flagKeys = new string[] { "flag1", "flag2", "flag3" };

        // Response with 3 flags
        var response = new FlagConfigurationResponse()
        {
            Bandits = null,
            Flags = BasicFlags(flagKeys)
        };

        // Response with 0 flags
        var emptyResponse = new FlagConfigurationResponse()
        {
            Bandits = null,
            Flags = new Dictionary<string, Flag> { }
        };

        mockAPI.Setup(m => m.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT, It.IsNotIn<string>(new string[] { "version1" })))
            .Returns(new VersionedResourceResponse<FlagConfigurationResponse>(response, "version1"));

        // Return an empty response with `isModified` = false. If the `ConfigurationRequester` does not heed `isModified`, 
        // the flags in `flagKeys` will be not present.
        mockAPI.Setup(m => m.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT, "version1"))
            .Returns(new VersionedResourceResponse<FlagConfigurationResponse>(emptyResponse, "version1", isModified: false));

        var store = CreateConfigurationStore();

        var requester = new ConfigurationRequester(mockAPI.Object, store);

        requester.LoadConfiguration(); // sends null as lastversion


        mockAPI.Verify(m => m.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT, null), Times.Exactly(1));

        Assert.Multiple(() =>
        {
            foreach (var flagKey in flagKeys)
            {
                Assert.That(requester.TryGetFlag(flagKey, out Flag? flag), Is.True);
                Assert.That(flag, Is.Not.Null);
                Assert.That(flag!.Key, Is.EqualTo(flagKey));
            }
        });

        requester.LoadConfiguration(); // sends version1 as lastversion

        mockAPI.Verify(m => m.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT, "version1"), Times.Exactly(1));

        // All the flags should be available, despite the response being empty.
        Assert.Multiple(() =>
        {
            foreach (var flagKey in flagKeys)
            {
                Assert.That(requester.TryGetFlag(flagKey, out Flag? flag), Is.True);
                Assert.That(flag, Is.Not.Null);
                Assert.That(flag!.Key, Is.EqualTo(flagKey));
            }
        });
    }

    private static Dictionary<string, Flag> BasicFlags(string[] flagKeys)
    {
        return flagKeys.ToDictionary(fk => fk, fk => BasicFlag(fk, Array.Empty<string>()));
    }

    [Test]
    public void ShouldResetFlagsOnLoad()
    {
        var flags1 = new string[] { "flag1", "flag2", "flag3" };
        var flags2 = new string[] { "flag1", "flag3", "flag4" };
        var flags3 = new string[] { "flag5", "flag6" };

        var unchangingBanditVariation = new BanditVariation("unchangingBandit", "flag1", "unchangingBandit", "unchangingBandit");
        var departingBanditVariation = new BanditVariation("departingBandit", "flag2", "departingBandit", "departingBandit");
        var newBanditVariation = new BanditVariation("newBandit", "flag4", "newBandit", "newBandit");
        var newBandit2Variation = new BanditVariation("newBandit2", "flag6", "newBandit2", "newBandit2");

        var banditFlags1 = new BanditFlags()
        {
            ["unchangingBandit"] = new BanditVariation[] { unchangingBanditVariation },
            ["departingBandit"] = new BanditVariation[] { departingBanditVariation },
        };
        var banditFlags2 = new BanditFlags()
        {
            ["unchangingBandit"] = new BanditVariation[] { unchangingBanditVariation },
            ["newBandit"] = new BanditVariation[] { newBanditVariation },
        };
        var banditFlags3 = new BanditFlags()
        {
            ["unchangingBandit"] = new BanditVariation[] { unchangingBanditVariation },
            ["newBandit2"] = new BanditVariation[] { newBandit2Variation },
        };

        var bandits1 = new string[] { "unchangingBandit", "departingBandit" };
        var bandits2 = new string[] { "unchangingBandit", "newBandit" };
        var bandits3 = new string[] { "unchangingBandit", "newBandit2" };


        var response1 = new FlagConfigurationResponse()
        {
            Bandits = banditFlags1,
            Flags = BasicFlags(flags1)
        };
        var response2 = new FlagConfigurationResponse()
        {
            Bandits = banditFlags2,
            Flags = BasicFlags(flags2)
        };
        var response3 = new FlagConfigurationResponse()
        {
            Bandits = banditFlags3,
            Flags = BasicFlags(flags3)
        };

        var banditResponse1 = new BanditModelResponse()
        {
            Bandits = bandits1.ToDictionary(b => b, b => BasicBandit(b))
        };
        var banditResponse2 = new BanditModelResponse()
        {
            Bandits = bandits2.ToDictionary(b => b, b => BasicBandit(b))
        };
        var banditResponse3 = new BanditModelResponse()
        {
            Bandits = bandits3.ToDictionary(b => b, b => BasicBandit(b))
        };


        // Set up the API to return the 3 responses in order.
        var mockAPI = new Mock<EppoHttpClient>("apiKey", "sdkName", "sdkVersion", "baseUrl");
        mockAPI.SetupSequence(m => m.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT, It.IsAny<string>()))
            .Returns(new VersionedResourceResponse<FlagConfigurationResponse>(response1, "version1"))
            .Returns(new VersionedResourceResponse<FlagConfigurationResponse>(response2, "version2"))
            .Returns(new VersionedResourceResponse<FlagConfigurationResponse>(response3, "version3"));
        mockAPI.SetupSequence(m => m.Get<BanditModelResponse>(Constants.BANDIT_ENDPOINT, It.IsAny<string>()))
            .Returns(new VersionedResourceResponse<BanditModelResponse>(banditResponse1, "version1"))
            .Returns(new VersionedResourceResponse<BanditModelResponse>(banditResponse2, "version2"))
            .Returns(new VersionedResourceResponse<BanditModelResponse>(banditResponse3, "version3"));

        var store = CreateConfigurationStore();
        var requester = new ConfigurationRequester(mockAPI.Object, store);

        // First load = config sets #1
        requester.LoadConfiguration();
        AssertHasConfig(requester, flags1, banditFlags1, bandits1);

        // second load = config sets #2
        requester.LoadConfiguration();
        AssertHasConfig(requester, flags2, banditFlags2, bandits2);

        // third load = config sets #3
        requester.LoadConfiguration();
        AssertHasConfig(requester, flags3, banditFlags3, bandits3);
    }

    private static void AssertHasConfig(ConfigurationRequester requester, string[] flagKeys, BanditFlags banditFlags, string[] banditKeys)
    {
        Assert.Multiple(() =>
        {
            foreach (var flagKey in flagKeys)
            {
                Assert.That(requester.TryGetFlag(flagKey, out Flag? flag), Is.True);
                Assert.That(flag, Is.Not.Null);
                Assert.That(flag!.Key, Is.EqualTo(flagKey));
            }
            Assert.That(requester.GetBanditFlags(), Is.EqualTo(banditFlags));
            foreach (var banditKey in banditKeys)
            {
                Assert.That(requester.TryGetBandit(banditKey, out Bandit? bandit), Is.True);
                Assert.That(bandit, Is.Not.Null);
                Assert.That(bandit!.BanditKey, Is.EqualTo(banditKey));
            }
        });
    }
}
