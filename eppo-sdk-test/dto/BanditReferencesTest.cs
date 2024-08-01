using eppo_sdk.dto;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test.dto;


public class BanditReferencesTest
{
    private BanditReferences banditRefs;
    [SetUp]
    public void SetUp()
    {
        banditRefs = new BanditReferences()
        {
            // Typical `BanditVariationDto` values where `variation` is duplicated across the VariationValue, VariationKey and BanditKey fields.
            ["variation"] = new BanditReference(
                "v123",
                new BanditFlagVariation[] { new("variation", "banditFlagKey", "allocationKey", "variation", "variation") }),
            ["banditKey"] = new BanditReference(
                "v123",
                new BanditFlagVariation[] { new("banditKey", "flagKey", "allocationKey", "variationKey", "variationValue") }),
        };
    }
    [Test]
    public void ShouldDetermineWhetherBanditsAreReferenced()
    {
        var malformedBanditRefs = new BanditReferences()
        {
            ["banditWithNoVariations"] = new BanditReference("v123", Array.Empty<BanditFlagVariation>())
        };

        Assert.Multiple(() =>
        {
            Assert.That(banditRefs.HasBanditReferences(), Is.True);
            Assert.That(malformedBanditRefs.HasBanditReferences(), Is.False);
        });
    }

    [Test]
    public void ShouldReturnFalseIfNotABanditVariation()
    {
        Multiple(() =>
        {
            // Neither match
            That(banditRefs.TryGetBanditKey("notAFlag", "notAVaration", out string? _), Is.False);
            // flag key is valid, but variation doesn't match
            That(banditRefs.TryGetBanditKey("flagKey", "notAVaration", out string? _), Is.False);
            // variation matches a bandit but the flag does not.
            That(banditRefs.TryGetBanditKey("notAFlag", "variation", out string? _), Is.False);
        });
    }

    [Test]
    public void ShouldLookupBanditKey()
    {
        Multiple(() =>
        {
            That(banditRefs.TryGetBanditKey("flagKey", "variationValue", out string? key1), Is.True);
            That(key1, Is.EqualTo("banditKey"));

            That(banditRefs.TryGetBanditKey("banditFlagKey", "variation", out string? key2), Is.True);
            That(key2, Is.EqualTo("variation"));
        });
    }
}
