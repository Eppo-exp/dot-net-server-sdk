using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.helpers;
using eppo_sdk.store;
using NUnit.Framework.Internal;
using static NUnit.Framework.Assert;


namespace eppo_sdk_test.store;

public class ConfigurationStoreTest
{
    private static ConfigurationStore CreateConfigurationStore()
    {
        var configCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        var modelCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
        var metadataCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;

        return new ConfigurationStore(configCache, modelCache, metadataCache);
    }

    [Test]
    public void ShouldClearOldValuesOnSet()
    {
        var store = CreateConfigurationStore();

        var flag1 = new Flag("flag1", true, new(), EppoValueType.NUMERIC, new(), 10000);
        var flag2 = new Flag("flag2", true, new(), EppoValueType.NUMERIC, new(), 10000);
        var flag3 = new Flag("flag3", true, new(), EppoValueType.NUMERIC, new(), 10000);

        var initialFlags = new Flag[] {
            flag1,flag2
         };

        var newFlags = new Flag[]
        {
            flag1, flag3
        };

        var bandit1 = new Bandit("bandit1", "falcon", DateTime.Now, "v123", new ModelData()
        {
            Coefficients = new Dictionary<string, ActionCoefficients>()
        });
        var bandit2 = new Bandit("bandit2", "falcon", DateTime.Now, "v456", new ModelData()
        {
            Coefficients = new Dictionary<string, ActionCoefficients>()
        });
        var bandit3 = new Bandit("bandit3", "falcon", DateTime.Now, "v789", new ModelData()
        {
            Coefficients = new Dictionary<string, ActionCoefficients>()
        });
        var initialBandits = new Bandit[] { bandit1, bandit2 };
        var newBandits = new Bandit[] { bandit1, bandit3 };

        var initialBanditFlags = new BanditFlags()
        {
            ["bandit1"] = new BanditVariation[] { new("bandit1", "flag1", "bandit1", "bandit1") },
            ["bandit2"] = new BanditVariation[] { new("bandit2", "flag2", "bandit2", "bandit2") }
        };
        var newBanditFlags = new BanditFlags()
        {
            ["bandit1"] = new BanditVariation[] { new("bandit1", "flag1", "bandit1", "bandit1") },
            ["bandit3"] = new BanditVariation[] { new("bandit3", "flag3", "bandit3", "bandit3") }
        };

        var initialMetadata = new Dictionary<string, object>()
        {
            ["UFC_VERSION"] = "UFCVersion1",
            ["BANDIT_VARIATIONS"] = initialBanditFlags
        };

        var newlMetadata = new Dictionary<string, object>()
        {
            ["UFC_VERSION"] = "UFCVersion2",
            ["BANDIT_VARIATIONS"] = newBanditFlags
        };

        store.SetConfiguration(initialFlags, initialBandits, initialMetadata);

        AssertHasFlag(store, "flag1");
        AssertHasFlag(store, "flag2");
        AssertHasFlag(store, "flag3", false);

        AssertHasBandit(store, "bandit1");
        AssertHasBandit(store, "bandit2");
        AssertHasBandit(store, "bandit3", false);

        Assert.Multiple(()=> {
            Assert.That(store.TryGetMetadata("UFC_VERSION", out string? data), Is.True);
            Assert.That(data, Is.EqualTo("UFCVersion1"));

            Assert.That(store.TryGetMetadata("BANDIT_VARIATIONS", out BanditFlags? banditFlags), Is.True);
            Assert.That(banditFlags, Is.Not.Null);
            Assert.That(banditFlags?["bandit1"], Is.Not.Null);
            Assert.That(banditFlags?["bandit1"], Has.Length.EqualTo(1));
            Assert.That(banditFlags?["bandit2"], Is.Not.Null);
            Assert.That(banditFlags?["bandit2"], Has.Length.EqualTo(1));
        });

        store.SetConfiguration(newFlags, newBandits, newlMetadata);

        AssertHasFlag(store, "flag1");
        AssertHasFlag(store, "flag2", false);
        AssertHasFlag(store, "flag3");

        AssertHasBandit(store, "bandit1");
        AssertHasBandit(store, "bandit2", false);
        AssertHasBandit(store, "bandit3");

        Assert.Multiple(()=> {
            Assert.That(store.TryGetMetadata("UFC_VERSION", out string? data), Is.True);
            Assert.That(data, Is.EqualTo("UFCVersion2"));

            Assert.That(store.TryGetMetadata("BANDIT_VARIATIONS", out BanditFlags? banditFlags), Is.True);
            Assert.That(banditFlags, Is.Not.Null);
            Assert.That(banditFlags?["bandit1"], Is.Not.Null);
            Assert.That(banditFlags?["bandit1"], Has.Length.EqualTo(1));
            Assert.That(banditFlags?["bandit3"], Is.Not.Null);
            Assert.That(banditFlags?["bandit3"], Has.Length.EqualTo(1));
        });

        store.SetConfiguration(Array.Empty<Flag>(), Array.Empty<Bandit>(), new Dictionary<string, object>());

        AssertHasFlag(store, "flag1", false);
        AssertHasFlag(store, "flag2", false);
        AssertHasFlag(store, "flag3", false);

        AssertHasBandit(store, "bandit1", false);
        AssertHasBandit(store, "bandit2", false);
        AssertHasBandit(store, "bandit3", false);
        Assert.Multiple(()=> {
            Assert.That(store.TryGetMetadata("UFC_VERSION", out string? data), Is.False);
            Assert.That(data, Is.Null);
            Assert.That(store.TryGetMetadata("BANDIT_VARIATIONS", out string? banditFlags), Is.False);
            Assert.That(banditFlags, Is.Null);
        });
        
    }

    private static void AssertHasFlag(ConfigurationStore store, string flagKey, bool expectToExist = true)
    {
        if (expectToExist)
        {
            Multiple(() =>
            {
                That(store.TryGetFlag(flagKey, out Flag? flag), Is.True);
                That(flag, Is.Not.Null);
                That(flag!.Key, Is.EqualTo(flagKey));
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

    private static void AssertHasBandit(ConfigurationStore store, string banditKey, bool expectToExist = true)
    {
        if (expectToExist)
        {
            Multiple(() =>
            {
                That(store.TryGetBandit(banditKey, out Bandit? bandit), Is.True);
                That(bandit, Is.Not.Null);
                That(bandit!.BanditKey, Is.EqualTo(banditKey));
            });
        }
        else
        {
            Multiple(() =>
            {
                That(store.TryGetBandit(banditKey, out Bandit? bandit), Is.False);
                That(bandit, Is.Null);
            });
        }
    }
}
