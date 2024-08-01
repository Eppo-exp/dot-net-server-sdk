using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test.dto;


public class BanditFlagsTest
{
    private BanditFlags banditFlags;
    [SetUp]
    public void SetUp()
    {
        banditFlags = new BanditFlags()
        {
            // Typical `BanditVariationDto` values where `variation` is duplicated across the VariationValue, VariationKey and BanditKey fields.
            ["variation"] = new BanditVariation[] { new("variation", "banditFlagKey", "variation", "variation") },

            ["banditKey"] = new BanditVariation[] { new("banditKey", "flagKey", "variationKey", "variationValue") }
        };
    }
    [Test]
    public void ShouldIndicateBanditFlags()
    {
        Multiple(() =>
        {
            That(banditFlags.IsBanditFlag("notAFlag"), Is.False);
            That(banditFlags.IsBanditFlag("banditFlagKey"), Is.True);

            That(banditFlags.IsBanditFlag("banditKey"), Is.False);
            That(banditFlags.IsBanditFlag("variationKey"), Is.False);
            That(banditFlags.IsBanditFlag("variationValue"), Is.False);
            That(banditFlags.IsBanditFlag("flagKey"), Is.True);
        });
    }

    [Test]
    public void ShouldReturnFalseIfNotABanditVariation()
    {
        Multiple(() =>
        {
            // Neither match
            That(banditFlags.TryGetBanditKey("notAFlag", "notAVaration", out string? _), Is.False);
            // flag key is valid, but variation doesn't match
            That(banditFlags.TryGetBanditKey("flagKey", "notAVaration", out string? _), Is.False);
            // variation matches a bandit but the flag does not.
            That(banditFlags.TryGetBanditKey("notAFlag", "variation", out string? _), Is.False);
        });
    }

    [Test]
    public void ShouldLookupBanditKey()
    {
        Multiple(() =>
        {
            That(banditFlags.TryGetBanditKey("flagKey", "variationValue", out string? key1), Is.True);
            That(key1, Is.EqualTo("banditKey"));

            That(banditFlags.TryGetBanditKey("banditFlagKey", "variation", out string? key2), Is.True);
            That(key2, Is.EqualTo("variation"));
        });
    }
}
