using eppo_sdk.helpers;

namespace eppo_sdk_test.helpers;

public class ShardTest
{
    [Test]
    public void ShouldReturnHexString()
    {
        Assert.That(Shard.GetHex("hello-world"), Is.EqualTo("2095312189753DE6AD47DFE20CBE97EC"));
        Assert.That(Shard.GetHex("another-string-with-experiment-subject"), Is.EqualTo("FD6BFC667B1BCDB901173F3D712E6C50"));
    }

    [Test]
    public void ShouldReturnShard()
    {
        const int maxShards = 100;
        var shardValue = Shard.GetShard("test-user", maxShards);
        Assert.That(shardValue, Is.GreaterThanOrEqualTo(0));
        Assert.That(shardValue, Is.LessThanOrEqualTo(maxShards));
    }
}