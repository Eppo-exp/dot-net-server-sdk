using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.logger;

namespace eppo_sdk;
public class EppoClientConfig
{
    public string ApiKey;
    public readonly IAssignmentLogger AssignmentLogger;

    public EppoClientConfig(string apiKey,
                            IAssignmentLogger? assignmentLogger)
    {
        ApiKey = apiKey;
        AssignmentLogger = assignmentLogger ?? new DefaultLogger();
    }

    public string BaseUrl { get; set; } = Constants.DEFAULT_BASE_URL;

    private long? _pollingIntervalInMillis;
    public long PollingIntervalInMillis
    {
        get => _pollingIntervalInMillis ?? Constants.TIME_INTERVAL_IN_MILLIS;
        set => _pollingIntervalInMillis = value;
    }

    private long? _pollingJitterInMillis;
    public long PollingJitterInMillis
    {
        get => _pollingJitterInMillis ?? Constants.JITTER_INTERVAL_IN_MILLIS;
        set => _pollingJitterInMillis = value;
    }


    internal class DefaultLogger : IAssignmentLogger
    {
        public void LogAssignment(AssignmentLogData assignmentLogData)
        {
            // noop
        }

        public void LogBanditAction(BanditLogEvent banditLogEvent)
        {
            // noop
        }
    }
}
