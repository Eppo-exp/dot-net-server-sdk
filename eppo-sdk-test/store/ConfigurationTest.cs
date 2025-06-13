using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.http;
using eppo_sdk.store;
using NUnit.Framework.Internal;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test.store;

public class ConfigurationTest
{
    private static Flag CreateFlag(string key, string[] variationValues)
    {
        var variations = variationValues
            .Select((v) => new Variation(v, v))
            .ToDictionary(v => v.Key);
        return new Flag(
            key,
            true,
            new List<Allocation>(),
            EppoValueType.STRING,
            variations,
            10_000
        );
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
        string version,
        BanditReferences? banditReferences = null
    )
    {
        return new Configuration(
            new VersionedResourceResponse<FlagConfigurationResponse>(
                new FlagConfigurationResponse
                {
                    Flags = flags,
                    BanditReferences = banditReferences,
                },
                version
            ),
            new VersionedResourceResponse<BanditModelResponse>(
                new BanditModelResponse { Bandits = bandits },
                version
            )
        );
    }

    [Test]
    public void ShouldCreateEmptyConfiguration()
    {
        var config = Configuration.Empty;

        Assert.Multiple(() =>
        {
            Assert.That(config.TryGetFlag("any", out Flag? flag), Is.False);
            Assert.That(flag, Is.Null);

            Assert.That(config.TryGetBandit("any", out Bandit? bandit), Is.False);
            Assert.That(bandit, Is.Null);

            Assert.That(config.GetFlagConfigVersion(), Is.Null);
            Assert.That(config.GetBanditModelVersions(), Is.Empty);
        });
    }

    [Test]
    public void ShouldCreateConfigurationFromVersionedResponses()
    {
        var flags = new Dictionary<string, Flag>
        {
            ["flag1"] = CreateFlag("flag1", new string[] { "control", "bandit1" }),
        };
        var banditReferences = new BanditReferences()
        {
            ["bandit1"] = new BanditReference(
                "v123",
                new BanditFlagVariation[]
                {
                    new("bandit1", "flag1", "allocation", "bandit1", "bandit1"),
                }
            ),
        };
        var bandits = new Dictionary<string, Bandit> { ["bandit1"] = CreateBandit("bandit1") };

        var config = CreateConfiguration(flags, bandits, "version1", banditReferences);

        Assert.Multiple(() =>
        {
            Assert.That(config.TryGetFlag("flag1", out Flag? flag), Is.True);
            Assert.That(flag, Is.Not.Null);
            Assert.That(flag!.Key, Is.EqualTo("flag1"));

            Assert.That(config.TryGetBandit("bandit1", out Bandit? bandit), Is.True);
            Assert.That(bandit, Is.Not.Null);
            Assert.That(bandit!.BanditKey, Is.EqualTo("bandit1"));

            Assert.That(config.GetFlagConfigVersion(), Is.EqualTo("version1"));
            Assert.That(config.GetBanditModelVersions().Count(), Is.EqualTo(1));
            Assert.That(config.GetBanditModelVersions(), Does.Contain("v123"));
        });
    }

    [Test]
    public void ShouldCreateConfigurationWithNewFlags()
    {
        var initialFlags = new Dictionary<string, Flag>
        {
            ["flag1"] = CreateFlag("flag1", new string[] { "control", "bandit1" }),
        };
        var initialBandits = new Dictionary<string, Bandit>
        {
            ["bandit1"] = CreateBandit("bandit1"),
        };
        var initialConfig = CreateConfiguration(initialFlags, initialBandits, "version1");

        var newFlags = new Dictionary<string, Flag>
        {
            ["flag2"] = CreateFlag("flag2", new string[] { "control", "bandit2" }),
        };
        var newFlagResponse = new FlagConfigurationResponse()
        {
            BanditReferences = null,
            Flags = newFlags,
        };

        var newConfig = initialConfig.WithNewFlags(
            new VersionedResourceResponse<FlagConfigurationResponse>(newFlagResponse, "version2")
        );

        Assert.Multiple(() =>
        {
            // Old flag should be gone
            Assert.That(newConfig.TryGetFlag("flag1", out Flag? oldFlag), Is.False);
            Assert.That(oldFlag, Is.Null);

            // New flag should be present
            Assert.That(newConfig.TryGetFlag("flag2", out Flag? newFlag), Is.True);
            Assert.That(newFlag, Is.Not.Null);
            Assert.That(newFlag!.Key, Is.EqualTo("flag2"));

            // Bandits should be preserved
            Assert.That(newConfig.TryGetBandit("bandit1", out Bandit? bandit), Is.True);
            Assert.That(bandit, Is.Not.Null);
            Assert.That(bandit!.BanditKey, Is.EqualTo("bandit1"));

            // Version should be updated
            Assert.That(newConfig.GetFlagConfigVersion(), Is.EqualTo("version2"));
            Assert.That(newConfig.GetBanditModelVersions().Count(), Is.EqualTo(1));
            Assert.That(newConfig.GetBanditModelVersions(), Does.Contain("v123"));
        });
    }

    [Test]
    public void ShouldGetBanditByVariation()
    {
        var flags = new Dictionary<string, Flag>
        {
            ["flag1"] = CreateFlag("flag1", new string[] { "control", "bandit1" }),
        };
        var banditReferences = new BanditReferences()
        {
            ["bandit1"] = new BanditReference(
                "v123",
                new BanditFlagVariation[]
                {
                    new("bandit1", "flag1", "allocation", "bandit1", "bandit1"),
                }
            ),
        };
        var bandits = new Dictionary<string, Bandit> { ["bandit1"] = CreateBandit("bandit1") };

        var config = CreateConfiguration(flags, bandits, "version1", banditReferences);

        Assert.Multiple(() =>
        {
            // Should find bandit for matching flag and variation
            Assert.That(
                config.TryGetBanditByVariation("flag1", "bandit1", out Bandit? bandit),
                Is.True
            );
            Assert.That(bandit, Is.Not.Null);
            Assert.That(bandit!.BanditKey, Is.EqualTo("bandit1"));

            // Should not find bandit for non-matching variation
            Assert.That(
                config.TryGetBanditByVariation("flag1", "control", out Bandit? controlBandit),
                Is.False
            );
            Assert.That(controlBandit, Is.Null);

            // Should not find bandit for non-existent flag
            Assert.That(
                config.TryGetBanditByVariation(
                    "nonexistent",
                    "bandit1",
                    out Bandit? nonexistentBandit
                ),
                Is.False
            );
            Assert.That(nonexistentBandit, Is.Null);
        });
    }
}
