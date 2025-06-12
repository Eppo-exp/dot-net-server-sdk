using eppo_sdk.http;
using NLog;

namespace eppo_sdk.tasks;

public class FetchExperimentsTask : IDisposable
{
    private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();

    private readonly long TimeIntervalInMillis;
    private readonly long JitterTimeIntervalInMillis;
    private readonly IConfigurationRequester ConfigLoader;
    private readonly Timer Timer;

    public FetchExperimentsTask(
        IConfigurationRequester config,
        long timeIntervalInMillis,
        long jitterTimeIntervalInMillis
    )
    {
        ConfigLoader = config;
        TimeIntervalInMillis = timeIntervalInMillis;
        JitterTimeIntervalInMillis = jitterTimeIntervalInMillis;

        Timer = new Timer(state => Run(), null, timeIntervalInMillis, Timeout.Infinite);
    }

    internal void Run()
    {
        long jitter = 0;
        if (JitterTimeIntervalInMillis > 0)
        {
            var rnd = new Random();
            jitter = rnd.Next(1, unchecked((int)JitterTimeIntervalInMillis));
        }

        var nextTick = TimeIntervalInMillis - jitter;

        Timer.Change(nextTick, Timeout.Infinite);
        try
        {
            ConfigLoader.LoadConfiguration();
        }
        catch (Exception e)
        {
            s_logger.Error("Error occured polling for configuration, " + e.Message);
        }
    }

    public void Dispose()
    {
        Timer.Dispose();
    }
}
