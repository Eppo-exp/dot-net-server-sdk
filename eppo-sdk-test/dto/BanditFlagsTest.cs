
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
            ["banditKey"] = new BanditVariation[]{ new BanditVariation("banditKey", "banditFlagKey", "variationKey", "variationValue")}
        };

        Multiple(() =>
        {
            That(banditFlags.IsBanditFlag("notAFlag"), Is.False);
            That(banditFlags.IsBanditFlag("banditFlagKey"), Is.True);
        });
    }
}
