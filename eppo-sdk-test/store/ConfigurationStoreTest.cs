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

    [Test]
    public void ShouldClearOldValuesOnSet()
    {
        var store = CreateConfigurationStore();

        var flag1 = new Flag("flag1", true, new(), EppoValueType.NUMERIC, new(), 10000);
        var flag2 = new Flag("flag2", true, new(), EppoValueType.NUMERIC, new(), 10000);
        var flag3 = new Flag("flag3", true, new(), EppoValueType.NUMERIC, new(), 10000);

        var initialFlags = new Dictionary<string, Flag> { ["flag1"] = flag1, ["flag2"] = flag2 };
        var newFlags = new Dictionary<string, Flag> { ["flag1"] = flag1, ["flag3"] = flag3 };

        var bandit1 = new Bandit(
            "bandit1",
            "falcon",
            DateTime.Now,
            "v123",
            new ModelData() { Coefficients = new Dictionary<string, ActionCoefficients>() }
        );
        var bandit2 = new Bandit(
            "bandit2",
            "falcon",
            DateTime.Now,
            "v456",
            new ModelData() { Coefficients = new Dictionary<string, ActionCoefficients>() }
        );
        var bandit3 = new Bandit(
            "bandit3",
            "falcon",
            DateTime.Now,
            "v789",
            new ModelData() { Coefficients = new Dictionary<string, ActionCoefficients>() }
        );
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

        var initialConfig = new Configuration(
            new VersionedResourceResponse<FlagConfigurationResponse>(
                new FlagConfigurationResponse { Flags = initialFlags },
                "version1"
            ),
            new VersionedResourceResponse<BanditModelResponse>(
                new BanditModelResponse { Bandits = initialBandits },
                "version1"
            )
        );

        var newConfig = new Configuration(
            new VersionedResourceResponse<FlagConfigurationResponse>(
                new FlagConfigurationResponse { Flags = newFlags },
                "version2"
            ),
            new VersionedResourceResponse<BanditModelResponse>(
                new BanditModelResponse { Bandits = newBandits },
                "version2"
            )
        );

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

        var bandit1 = new Bandit(
            "bandit1",
            "falcon",
            DateTime.Now,
            "v123",
            new ModelData() { Coefficients = new Dictionary<string, ActionCoefficients>() }
        );
        var bandit2 = new Bandit(
            "bandit2",
            "falcon",
            DateTime.Now,
            "v456",
            new ModelData() { Coefficients = new Dictionary<string, ActionCoefficients>() }
        );
        var bandit3 = new Bandit(
            "bandit3",
            "falcon",
            DateTime.Now,
            "v789",
            new ModelData() { Coefficients = new Dictionary<string, ActionCoefficients>() }
        );
        var bandits = new Dictionary<string, Bandit>
        {
            ["bandit1"] = bandit1,
            ["bandit2"] = bandit2,
        };

        var initialConfig = new Configuration(
            new VersionedResourceResponse<FlagConfigurationResponse>(
                new FlagConfigurationResponse { Flags = flags },
                "version1"
            ),
            new VersionedResourceResponse<BanditModelResponse>(
                new BanditModelResponse { Bandits = bandits },
                "version1"
            )
        );

        var newConfig = new Configuration(
            new VersionedResourceResponse<FlagConfigurationResponse>(
                new FlagConfigurationResponse { Flags = flags },
                "version2"
            ),
            new VersionedResourceResponse<BanditModelResponse>(
                new BanditModelResponse
                {
                    Bandits = new Dictionary<string, Bandit> { ["bandit3"] = bandit3 },
                },
                "version2"
            )
        );

        var emptyConfig = new Configuration(
            new VersionedResourceResponse<FlagConfigurationResponse>(
                new FlagConfigurationResponse { Flags = flags },
                "version3"
            ),
            new VersionedResourceResponse<BanditModelResponse>(
                new BanditModelResponse { Bandits = new Dictionary<string, Bandit>() },
                "version3"
            )
        );

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
