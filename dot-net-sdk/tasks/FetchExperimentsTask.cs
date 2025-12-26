using eppo_sdk.http;
using NLog;

namespace eppo_sdk.tasks;

public class FetchExperimentsTask : IDisposable
{
    private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();

    private readonly long TimeIntervalInMillis;
    private readonly long JitterTimeIntervalInMillis;
    private readonly IConfigurationRequester ConfigLoader;
    private readonly ITimer Timer;
    private readonly TimeProvider TimeProvider;

    public FetchExperimentsTask(
        IConfigurationRequester config,
        long timeIntervalInMillis,
        long jitterTimeIntervalInMillis,
        TimeProvider? timeProvider = null
    )
    {
        ConfigLoader = config;
        TimeIntervalInMillis = timeIntervalInMillis;
        JitterTimeIntervalInMillis = jitterTimeIntervalInMillis;
        TimeProvider = timeProvider ?? TimeProvider.System;

        Timer = TimeProvider.CreateTimer(state => Run(), null, TimeSpan.FromMilliseconds(timeIntervalInMillis), Timeout.InfiniteTimeSpan);
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

        Timer.Change(TimeSpan.FromMilliseconds(nextTick), Timeout.InfiniteTimeSpan);
        try
        {
            ConfigLoader.FetchAndActivateConfiguration();
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
