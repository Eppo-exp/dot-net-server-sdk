using eppo_sdk.constants;
using eppo_sdk.dto;
using NLog;

namespace eppo_sdk.http;

public interface IConfigurationRequester
{
    public VersionedResourceResponse<FlagConfigurationResponse> FetchFlagConfiguration(string? lastEtag = null);
    public VersionedResourceResponse<BanditModelResponse> FetchBanditModels();
}
public class ConfigurationRequester : IConfigurationRequester
{
    private static Logger logger = LogManager.GetCurrentClassLogger();
    private readonly EppoHttpClient eppoHttpClient;

    public ConfigurationRequester(EppoHttpClient eppoHttpClient)
    {
        this.eppoHttpClient = eppoHttpClient;
    }

    public VersionedResourceResponse<FlagConfigurationResponse> FetchFlagConfiguration(string? lastEtag = null)
    {
        return eppoHttpClient.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT, lastEtag);

    }

    public VersionedResourceResponse<BanditModelResponse> FetchBanditModels()
    {
        return eppoHttpClient.Get<BanditModelResponse>(Constants.BANDIT_ENDPOINT);

    }
}
