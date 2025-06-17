using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.helpers;
using eppo_sdk.http;
using eppo_sdk.store;
using NUnit.Framework.Internal;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test.store;

public class ConfigurationStoreTest
{
    private static ConfigurationStore CreateConfigurationStore()
    {
        return new ConfigurationStore();
    }

    private static Flag CreateFlag(string key, EppoValueType valueType = EppoValueType.NUMERIC)
    {
        return new Flag(key, true, new(), valueType, new(), 10000);
    }

    private static Bandit CreateBandit(string key, string modelVersion = "v123")
    {
        return new Bandit(
            key,
            "falcon",
            DateTime.Now,
            modelVersion,
            new ModelData() { Coefficients = new Dictionary<string, ActionCoefficients>() }
        );
    }

    private static Configuration CreateConfiguration(
        Dictionary<string, Flag> flags,
        Dictionary<string, Bandit> bandits,
        string version
    )
    {
        return new Configuration(
            new VersionedResourceResponse<FlagConfigurationResponse>(
                new FlagConfigurationResponse { Flags = flags },
                version
            ),
            new VersionedResourceResponse<BanditModelResponse>(
                new BanditModelResponse { Bandits = bandits },
                version
            )
        );
    }

    [Test]
    public void ShouldClearOldValuesOnSet()
    {
        var store = CreateConfigurationStore();

        var flag1 = CreateFlag("flag1");
        var flag2 = CreateFlag("flag2");
        var flag3 = CreateFlag("flag3");

        var initialFlags = new Dictionary<string, Flag> { ["flag1"] = flag1, ["flag2"] = flag2 };
        var newFlags = new Dictionary<string, Flag> { ["flag1"] = flag1, ["flag3"] = flag3 };

        var bandit1 = CreateBandit("bandit1");
        var bandit2 = CreateBandit("bandit2", "v456");
        var bandit3 = CreateBandit("bandit3", "v789");

        var initialBandits = new Dictionary<string, Bandit>
        {
            ["bandit1"] = bandit1,
            ["bandit2"] = bandit2,
        };
        var newBandits = new Dictionary<string, Bandit>
        {
            ["bandit1"] = bandit1,
            ["bandit3"] = bandit3,
        };

        var initialConfig = CreateConfiguration(initialFlags, initialBandits, "version1");
        var newConfig = CreateConfiguration(newFlags, newBandits, "version2");
        var emptyConfig = Configuration.Empty;

        store.SetConfiguration(initialConfig);

        AssertHasFlag(store, "flag1");
        AssertHasFlag(store, "flag2");
        AssertHasFlag(store, "flag3", false);

        AssertHasBandit(store, "bandit1");
        AssertHasBandit(store, "bandit2");
        AssertHasBandit(store, "bandit3", false);

        Assert.Multiple(() =>
        {
            var config = store.GetConfiguration();
            Assert.That(config.GetFlagConfigVersion(), Is.EqualTo("version1"));
        });

        store.SetConfiguration(newConfig);

        AssertHasFlag(store, "flag1");
        AssertHasFlag(store, "flag2", false);
        AssertHasFlag(store, "flag3");

        AssertHasBandit(store, "bandit1");
        AssertHasBandit(store, "bandit2", false);
        AssertHasBandit(store, "bandit3");

        Assert.Multiple(() =>
        {
            var config = store.GetConfiguration();
            Assert.That(config.GetFlagConfigVersion(), Is.EqualTo("version2"));
        });

        store.SetConfiguration(emptyConfig);

        AssertHasFlag(store, "flag1", false);
        AssertHasFlag(store, "flag2", false);
        AssertHasFlag(store, "flag3", false);

        AssertHasBandit(store, "bandit1", false);
        AssertHasBandit(store, "bandit2", false);
        AssertHasBandit(store, "bandit3", false);

        Assert.Multiple(() =>
        {
            var config = store.GetConfiguration();
            Assert.That(config.GetFlagConfigVersion(), Is.Null);
        });
    }

    [Test]
    public void ShouldUpdateConfigPreservingBandits()
    {
        var store = CreateConfigurationStore();

        var flags = new Dictionary<string, Flag>();

        var bandit1 = CreateBandit("bandit1");
        var bandit2 = CreateBandit("bandit2", "v456");
        var bandit3 = CreateBandit("bandit3", "v789");

        var bandits = new Dictionary<string, Bandit>
        {
            ["bandit1"] = bandit1,
            ["bandit2"] = bandit2,
        };

        var initialConfig = CreateConfiguration(flags, bandits, "version1");
        var newConfig = CreateConfiguration(
            flags,
            new Dictionary<string, Bandit> { ["bandit3"] = bandit3 },
            "version2"
        );
        var emptyConfig = CreateConfiguration(flags, new Dictionary<string, Bandit>(), "version3");

        store.SetConfiguration(initialConfig);
        AssertHasBandit(store, "bandit1");
        AssertHasBandit(store, "bandit2");

        // Existing bandits should not be overwritten when only updating flags
        var currentConfig = store.GetConfiguration();
        var updatedConfig = currentConfig.WithNewFlags(
            new VersionedResourceResponse<FlagConfigurationResponse>(
                new FlagConfigurationResponse { Flags = flags },
                "version2"
            )
        );
        store.SetConfiguration(updatedConfig);

        AssertHasBandit(store, "bandit1");
        AssertHasBandit(store, "bandit2");
        AssertHasBandit(store, "bandit3", false);

        store.SetConfiguration(newConfig);

        AssertHasBandit(store, "bandit1", false);
        AssertHasBandit(store, "bandit2", false);
        AssertHasBandit(store, "bandit3");

        store.SetConfiguration(emptyConfig);

        AssertHasBandit(store, "bandit1", false);
        AssertHasBandit(store, "bandit2", false);
        AssertHasBandit(store, "bandit3", false);
    }

    private static void AssertHasFlag(
        ConfigurationStore store,
        string flagKey,
        bool expectToExist = true
    )
    {
        if (expectToExist)
        {
            Multiple(() =>
            {
                var config = store.GetConfiguration();
                That(config.TryGetFlag(flagKey, out Flag? flag), Is.True);
                That(flag, Is.Not.Null);
                That(flag!.Key, Is.EqualTo(flagKey));
            });
        }
        else
        {
            Multiple(() =>
            {
                var config = store.GetConfiguration();
                That(config.TryGetFlag(flagKey, out Flag? flag), Is.False);
                That(flag, Is.Null);
            });
        }
    }

    private static void AssertHasBandit(
        ConfigurationStore store,
        string banditKey,
        bool expectToExist = true
    )
    {
        if (expectToExist)
        {
            Multiple(() =>
            {
                var config = store.GetConfiguration();
                That(config.TryGetBandit(banditKey, out Bandit? bandit), Is.True);
                That(bandit, Is.Not.Null);
                That(bandit!.BanditKey, Is.EqualTo(banditKey));
            });
        }
        else
        {
            Multiple(() =>
            {
                var config = store.GetConfiguration();
                That(config.TryGetBandit(banditKey, out Bandit? bandit), Is.False);
                That(bandit, Is.Null);
            });
        }
    }
}
