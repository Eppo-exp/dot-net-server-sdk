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
        this.ApiKey = apiKey;
        this.AssignmentLogger = assignmentLogger ?? new DefaultLogger();
    }
    public string BaseUrl { get; set; } = Constants.DEFAULT_BASE_URL;

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
