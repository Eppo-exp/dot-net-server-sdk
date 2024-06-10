using eppo_sdk.helpers;

namespace eppo_sdk_test.helpers;

public class SharderTest
{
    [Test]
    public void ShouldReturnHexString()
    {
        Assert.That(Sharder.GetHex("hello-world"), Is.EqualTo("2095312189753de6ad47dfe20cbe97ec"));
        Assert.That(Sharder.GetHex("another-string-with-experiment-subject"), Is.EqualTo("fd6bfc667b1bcdb901173f3d712e6c50"));
    }

    [Test]
    public void ShouldReturnShard()
    {
        const int maxShards = 100;
        var shardValue = Sharder.GetShard("test-user", maxShards);
        Assert.That(shardValue, Is.GreaterThanOrEqualTo(0));
        Assert.That(shardValue, Is.LessThanOrEqualTo(maxShards));
    }
}