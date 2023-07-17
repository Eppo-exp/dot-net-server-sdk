using dot_net_eppo.dto;
using dot_net_sdk.constants;

namespace dot_net_sdk.http;

public class ExperimentConfigurationRequester
{
    private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    private readonly EppoHttpClient eppoHttpClient;

    public ExperimentConfigurationRequester(EppoHttpClient eppoHttpClient) {
        this.eppoHttpClient = eppoHttpClient;
    }

    public ExperimentConfigurationResponse? FetchExperimentConfiguration()
    {
        ExperimentConfigurationResponse? config = null;
        try
        {
            return this.eppoHttpClient.Get(Constants.RAC_ENDPOINT);
        }
        catch (UnauthorizedAccessException e)
        {
            throw e;
        }
        catch (Exception e)
        {
            logger.Warn($"Unable to Fetch Experiment Configuration: {e.Message}");
        }

        return config;
    }
}