
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test.dto;


public class BanditFlagsTest
{
    [Test]
    public void ShouldIndicateBanditFlags()
    {
        var banditFlags = new BanditFlags()
        {
            // Typical `BanditVariationDto` values where `variation` is duplicated across the VariationValue, VariationKey and BanditKey fields.
            ["variation"] = new BanditVariation[] {new("variation", "banditFlagKey", "variation", "variation")},

            ["banditKey"] = new BanditVariation[] {new("banditKey", "flagKey", "variationKey", "variationValue")}
        };

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
}
