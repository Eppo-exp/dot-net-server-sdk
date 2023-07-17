using dot_net_sdk.logger;

namespace dot_net_sdk
{
	public record EppoClientConfig(string apiKey, string baseUrl, IAssignmentLogger assignmentLogger);
}

