using eppo_sdk.store;

namespace eppo_sdk.tasks;

public class FetchExperimentsTask : IDisposable
{
    private long TimeIntervalInMillis;
    private long JitterTimeIntervalInMillis;
    private IConfigurationStore _configurationStore;
    private Timer timer;

    public FetchExperimentsTask(IConfigurationStore configurationStore, long timeIntervalInMillis,
        long jitterTimeIntervalInMillis)
    {
        _configurationStore = configurationStore;
        TimeIntervalInMillis = timeIntervalInMillis;
        JitterTimeIntervalInMillis = jitterTimeIntervalInMillis;
        timer = new Timer(state => Run(), null, timeIntervalInMillis, Timeout.Infinite);
    }

    internal void Run()
    {
        var rnd = new Random();
        var nextTick = TimeIntervalInMillis -
                       rnd.Next(1, unchecked((int)JitterTimeIntervalInMillis));
        timer.Change(nextTick, Timeout.Infinite);
        _configurationStore.FetchConfiguration();
    }

    public void Dispose()
    {
        timer.Dispose();
    }
}