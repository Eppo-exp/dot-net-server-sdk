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
        return new ConfigurationStore();
    }

    [Test]
    public void ShouldClearOldValuesOnSet()
    {
        var store = CreateConfigurationStore();

        var flag1 = new Flag("flag1", true, new(), EppoValueType.NUMERIC, new(), 10000);
        var flag2 = new Flag("flag2", true, new(), EppoValueType.NUMERIC, new(), 10000);
        var flag3 = new Flag("flag3", true, new(), EppoValueType.NUMERIC, new(), 10000);

        var initialFlags = new Flag[] { flag1, flag2 };

        var newFlags = new Flag[] { flag1, flag3 };

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
        var initialBandits = new Bandit[] { bandit1, bandit2 };
        var newBandits = new Bandit[] { bandit1, bandit3 };

        var banditReferences = new BanditReferences
        {
            ["bandit1"] = new BanditReference(
                "v123",
                new[]
                {
                    new BanditFlagVariation(
                        "bandit1",
                        "flag1",
                        "allocation1",
                        "variation1",
                        "variation1"
                    ),
                }
            ),
        };

        var initialMetadata = new Dictionary<string, object>()
        {
            ["ufcVersion"] = "UFCVersion1",
            ["banditReferences"] = banditReferences,
        };

        var newMetadata = new Dictionary<string, object>()
        {
            ["ufcVersion"] = "UFCVersion2",
            ["banditReferences"] = banditReferences,
        };

        store.SetConfiguration(initialFlags, initialBandits, initialMetadata);

        AssertHasFlag(store, "flag1");
        AssertHasFlag(store, "flag2");
        AssertHasFlag(store, "flag3", false);

        AssertHasBandit(store, "bandit1");
        AssertHasBandit(store, "bandit2");
        AssertHasBandit(store, "bandit3", false);

        store.SetConfiguration(newFlags, newBandits, newMetadata);

        AssertHasFlag(store, "flag1");
        AssertHasFlag(store, "flag2", false);
        AssertHasFlag(store, "flag3");

        AssertHasBandit(store, "bandit1");
        AssertHasBandit(store, "bandit2", false);
        AssertHasBandit(store, "bandit3");

        store.SetConfiguration(
            Array.Empty<Flag>(),
            Array.Empty<Bandit>(),
            new Dictionary<string, object>()
        );

        AssertHasFlag(store, "flag1", false);
        AssertHasFlag(store, "flag2", false);
        AssertHasFlag(store, "flag3", false);

        AssertHasBandit(store, "bandit1", false);
        AssertHasBandit(store, "bandit2", false);
        AssertHasBandit(store, "bandit3", false);
    }

    [Test]
    public void ShouldUpdateConfigPreservingBandits()
    {
        var store = CreateConfigurationStore();

        var flags = Array.Empty<Flag>();

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
        var bandits = new Bandit[] { bandit1, bandit2 };

        var dataDict = new Dictionary<string, object>
        {
            ["banditReferences"] = new BanditReferences(),
        };

        store.SetConfiguration(flags, bandits, dataDict);
        AssertHasBandit(store, "bandit1");
        AssertHasBandit(store, "bandit2");

        // Existing bandits should not be overwritten
        store.SetConfiguration(flags, dataDict);

        AssertHasBandit(store, "bandit1");
        AssertHasBandit(store, "bandit2");
        AssertHasBandit(store, "bandit3", false);

        store.SetConfiguration(flags, new Bandit[] { bandit3 }, dataDict);

        AssertHasBandit(store, "bandit1", false);
        AssertHasBandit(store, "bandit2", false);
        AssertHasBandit(store, "bandit3");

        store.SetConfiguration(flags, Array.Empty<Bandit>(), dataDict);

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
