using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.store;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test.store;

public class ConfigurationTest
{
    private Configuration _configuration;
    private Flag _testFlag;
    private Bandit _testBandit;
    private Dictionary<string, object> _testMetadata;

    [SetUp]
    public void SetUp()
    {
        _testFlag = new Flag(
            Key: "test-flag",
            Enabled: true,
            Allocations: new List<Allocation>(),
            VariationType: EppoValueType.STRING,
            Variations: new Dictionary<string, Variation>(),
            TotalShards: 10000
        );

        _testBandit = new Bandit(
            BanditKey: "test-bandit",
            ModelName: "test-model",
            UpdatedAt: DateTime.UtcNow,
            ModelVersion: "v1.0",
            ModelData: new ModelData
            {
                Gamma = 1.0,
                DefaultActionScore = 0.0,
                ActionProbabilityFloor = 0.0,
                Coefficients = new Dictionary<string, ActionCoefficients>()
            }
        );

        var banditReferences = new BanditReferences();
        banditReferences["test-bandit"] = new BanditReference(
            ModelVersion: "v1.0",
            FlagVariations: new[]
            {
                new BanditFlagVariation(
                    Key: "test-bandit",
                    FlagKey: "test-flag", 
                    AllocationKey: "allocation1",
                    VariationKey: "variation1",
                    VariationValue: "variation1"
                )
            }
        );

        _testMetadata = new Dictionary<string, object>
        {
            ["banditReferences"] = banditReferences,
            ["banditVersions"] = new List<string> { "v1.0", "v1.1" },
            ["ufcVersion"] = "1.0.0",
            ["customKey"] = "customValue"
        };

        _configuration = new Configuration(
            new[] { _testFlag },
            new[] { _testBandit },
            _testMetadata
        );
    }

    [Test]
    public void ShouldRetrieveFlag()
    {
        Multiple(() =>
        {
            That(_configuration.TryGetFlag("test-flag", out Flag? flag), Is.True);
            That(flag, Is.Not.Null);
            That(flag!.Key, Is.EqualTo("test-flag"));
        });
    }

    [Test]
    public void ShouldReturnFalseForNonExistentFlag()
    {
        Multiple(() =>
        {
            That(_configuration.TryGetFlag("non-existent", out Flag? flag), Is.False);
            That(flag, Is.Null);
        });
    }

    [Test]
    public void ShouldRetrieveBandit()
    {
        Multiple(() =>
        {
            That(_configuration.TryGetBandit("test-bandit", out Bandit? bandit), Is.True);
            That(bandit, Is.Not.Null);
            That(bandit!.BanditKey, Is.EqualTo("test-bandit"));
        });
    }

    [Test]
    public void ShouldReturnFalseForNonExistentBandit()
    {
        Multiple(() =>
        {
            That(_configuration.TryGetBandit("non-existent", out Bandit? bandit), Is.False);
            That(bandit, Is.Null);
        });
    }

    [Test]
    public void ShouldRetrieveBanditReferences()
    {
        Multiple(() =>
        {
            That(_configuration.TryGetBanditReferences(out BanditReferences? banditReferences), Is.True);
            That(banditReferences, Is.Not.Null);
            That(_configuration.BanditReferences, Is.Not.Null);
        });
    }

    [Test]
    public void ShouldRetrieveBanditVersions()
    {
        Multiple(() =>
        {
            That(_configuration.TryGetBanditVersions(out IEnumerable<string>? banditVersions), Is.True);
            That(banditVersions, Is.Not.Null);
            That(banditVersions!.Count(), Is.EqualTo(2));
            That(_configuration.BanditVersions.Count(), Is.EqualTo(2));
            That(_configuration.BanditVersions.Contains("v1.0"), Is.True);
            That(_configuration.BanditVersions.Contains("v1.1"), Is.True);
        });
    }

    [Test]
    public void ShouldRetrieveFlagConfigVersion()
    {
        Multiple(() =>
        {
            That(_configuration.TryGetFlagConfigVersion(out string? flagConfigVersion), Is.True);
            That(flagConfigVersion, Is.EqualTo("1.0.0"));
            That(_configuration.FlagConfigVersion, Is.EqualTo("1.0.0"));
        });
    }

    [Test]
    public void ShouldRetrieveMetadata()
    {
        Multiple(() =>
        {
            That(_configuration.TryGetMetadata<string>("customKey", out string? metadata), Is.True);
            That(metadata, Is.EqualTo("customValue"));
        });
    }

    [Test]
    public void ShouldReturnFalseForNonExistentMetadata()
    {
        Multiple(() =>
        {
            That(_configuration.TryGetMetadata<string>("non-existent", out string? metadata), Is.False);
            That(metadata, Is.Null);
        });
    }

    [Test]
    public void ShouldReturnFalseForWrongMetadataType()
    {
        Multiple(() =>
        {
            That(_configuration.TryGetMetadata<int>("ufcVersion", out int metadata), Is.False);
            That(metadata, Is.EqualTo(0));
        });
    }

    [Test]
    public void ShouldProvideImmutableCollections()
    {
        Multiple(() =>
        {
            That(_configuration.Flags.Count(), Is.EqualTo(1));
            That(_configuration.Flags.First().Key, Is.EqualTo("test-flag"));

            That(_configuration.Bandits.Count(), Is.EqualTo(1));
            That(_configuration.Bandits.First().BanditKey, Is.EqualTo("test-bandit"));

            That(_configuration.Metadata, Has.Count.EqualTo(4));
            That(_configuration.Metadata["ufcVersion"], Is.EqualTo("1.0.0"));
        });
    }

    [Test]
    public void ShouldHandleMissingSpecificMetadata()
    {
        var emptyConfiguration = new Configuration(
            Array.Empty<Flag>(),
            Array.Empty<Bandit>(),
            new Dictionary<string, object>()
        );

        Multiple(() =>
        {
            That(emptyConfiguration.TryGetBanditReferences(out _), Is.False);
            That(emptyConfiguration.BanditReferences, Is.Null);

            That(emptyConfiguration.TryGetBanditVersions(out _), Is.False);
            That(emptyConfiguration.BanditVersions, Is.Empty);

            That(emptyConfiguration.TryGetFlagConfigVersion(out _), Is.False);
            That(emptyConfiguration.FlagConfigVersion, Is.Null);
        });
    }
} 