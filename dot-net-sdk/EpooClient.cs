using dot_net_sdk.exception;

namespace dot_net_sdk;

public class EppoClient
{
    private static EppoClient _instance = null!;
    private ConfigurationStore configurationStore;
    private Timer poller;
    private EppoClientConfig eppoClientConfig;

    private EppoClient(ConfigurationStore configurationStore, Timer poller, EppoClientConfig eppoClientConfig)
    {
        this.configurationStore = configurationStore;
        this.poller = poller;
        this.eppoClientConfig = eppoClientConfig;
    }

    public static EppoClient init(EppoClientConfig eppoClientConfig)
    {
        lock(_instance)
        {
            InputValidator.validateNotBlank(eppoClientConfig.apiKey,
                                            "An API key is required");
        }
        return _instance;
    }

    public static EppoClient getInstance()
    {
        if (EppoClient._instance == null)
        {
            throw new EppoClientIsNotInitializedException("Eppo client not initiazed.");
        }
        return EppoClient._instance;
    }
}