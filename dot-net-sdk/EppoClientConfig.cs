using eppo_sdk.constants;
using eppo_sdk.logger;

namespace eppo_sdk
{
    public record EppoClientConfig(string ApiKey,
        IAssignmentLogger AssignmentLogger)
    {
        public string BaseUrl { get; set; } = Constants.DEFAULT_BASE_URL;
    }
}