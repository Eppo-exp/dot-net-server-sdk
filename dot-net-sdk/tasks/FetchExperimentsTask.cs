using eppo_sdk.http;

namespace eppo_sdk.tasks;

public class FetchExperimentsTask : IDisposable
{
    private readonly long TimeIntervalInMillis;
    private readonly long JitterTimeIntervalInMillis;
    private readonly IConfigurationRequester ConfigLoader;
    private readonly Timer Timer;

    public FetchExperimentsTask(IConfigurationRequester config,
                                long timeIntervalInMillis,
                                long jitterTimeIntervalInMillis)
    {
        ConfigLoader = config;
        TimeIntervalInMillis = timeIntervalInMillis;
        JitterTimeIntervalInMillis = jitterTimeIntervalInMillis;
        Timer = new Timer(state => Run(), null, timeIntervalInMillis, Timeout.Infinite);
    }

    internal void Run()
    {
        var rnd = new Random();
        var nextTick = TimeIntervalInMillis -
                       rnd.Next(1, unchecked((int)JitterTimeIntervalInMillis));
        Timer.Change(nextTick, Timeout.Infinite);
        ConfigLoader.LoadConfiguration();
    }

    public void Dispose()
    {
        Timer.Dispose();
    }
}
