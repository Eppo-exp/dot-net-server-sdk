using eppo_sdk.logger;

namespace eppo_sdk
{
	public record EppoClientConfig(string ApiKey, string BaseUrl, IAssignmentLogger AssignmentLogger);
}

