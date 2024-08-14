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

        // var interval = TimeSpan.FromMilliseconds(timeIntervalInMillis);

        // Scheduler.ScheduleTask(_ => Run(), interval);

        Timer = new Timer(
                state => Run(),
                null,
                timeIntervalInMillis,
                Timeout.Infinite);
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

        // Scheduler.ScheduleTask(_ => Run(), TimeSpan.FromMilliseconds(nextTick));
        Timer.Change(nextTick, Timeout.Infinite);
        ConfigLoader.LoadConfiguration();
    }

    public void Dispose()
    {
        // Scheduler.Dispose();
        Timer.Dispose();
    }
}

// public class Scheduler
// {
//     private static TimerCallback? _callback;
//     private static Timer? _timer;

//     public static void ScheduleTask(TimerCallback callback, TimeSpan interval)
//     {
//         _callback = callback;
//         if (_timer == null)
//         {
//             _timer = new Timer(callback, null, interval, Timeout.InfiniteTimeSpan);
//         }
//         else
//         {
//             _timer.Change(interval, Timeout.InfiniteTimeSpan);
//         }
//     }

//     /// <summary>
//     /// For testing only. This method invokes the scheduled callback immediately instead of waiting for time to pass.
//     /// </summary>
//     public static void DoNextCallback()
//     {
//         _callback?.Invoke(null);
//     }

//     internal static void Dispose()
//     {
//         _timer?.Dispose();
//         _timer = null;
//     }
// }
