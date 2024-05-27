using eppo_sdk.constants;
using eppo_sdk.dto;
using NLog;

namespace eppo_sdk.http;

public class ExperimentConfigurationRequester
{
    private static Logger logger = LogManager.GetCurrentClassLogger();
    private readonly EppoHttpClient eppoHttpClient;

    public ExperimentConfigurationRequester(EppoHttpClient eppoHttpClient) {
        this.eppoHttpClient = eppoHttpClient;
    }

    public ExperimentConfigurationResponse? FetchExperimentConfiguration()
    {
        try
        {
            return this.eppoHttpClient.Get(Constants.RAC_ENDPOINT);
        }
        catch (Exception e)
        {
            logger.Warn($"Unable to Fetch Experiment Configuration: {e.Message}");
        }

        return null;
    }
}