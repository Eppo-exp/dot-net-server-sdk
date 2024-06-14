using eppo_sdk.http;

namespace eppo_sdk_test.http;

[TestFixture]
public class ConfigurationRequesterTest
{
    [Test]
    public void ShouldHaveUniqueUidPerUrl() {
        var ht1 = new EppoHttpClient("","","","http://url1");
        var ht2 = new EppoHttpClient("","","","http://url2");

        var cr1 = new ConfigurationRequester(ht1);
        var cr2 = new ConfigurationRequester(ht2);
        Assert.That(cr1.UID, Is.Not.EqualTo(cr2.UID));

        // Same URL as ht1/cr1
        var ht3 = new EppoHttpClient("","","","http://url1");
        var cr3 = new ConfigurationRequester(ht3);

        Assert.That(cr1.UID, Is.EqualTo(cr3.UID));
    }
}