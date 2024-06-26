using eppo_sdk.constants;
using eppo_sdk.dto;
using NLog;

namespace eppo_sdk.http;

public interface IConfigurationRequester {
    public FlagConfigurationResponse? FetchFlagConfiguration();
    public BanditModelResponse? FetchBanditModels();
}
public class ConfigurationRequester : IConfigurationRequester
{
    private static Logger logger = LogManager.GetCurrentClassLogger();
    private readonly EppoHttpClient eppoHttpClient;

    public ConfigurationRequester(EppoHttpClient eppoHttpClient) {
        this.eppoHttpClient = eppoHttpClient;
    }

    public FlagConfigurationResponse? FetchFlagConfiguration()
    {
        try
        {
            return this.eppoHttpClient.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT);
        }
        catch (Exception e)
        {
            logger.Warn($"[Eppo SDK] Unable to Fetch Flag Configuration: {e.Message}");
        }

        return null;
    }

    public BanditModelResponse? FetchBanditModels()
    {
        try
        {
            return this.eppoHttpClient.Get<BanditModelResponse>(Constants.BANDIT_ENDPOINT);
        }
        catch (Exception e)
        {
            logger.Warn($"[Eppo SDK] Unable to Fetch Bandit Models: {e.Message}");
        }

        return null;
    }
}
