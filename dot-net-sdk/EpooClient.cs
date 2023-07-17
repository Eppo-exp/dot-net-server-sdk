using dot_net_eppo.dto;
using dot_net_sdk.constants;
using dot_net_sdk.exception;
using dot_net_sdk.helpers;
using dot_net_sdk.http;
using dot_net_sdk.store;

namespace dot_net_sdk;

public class EppoClient
{
    private static EppoClient Instance = null!;
    private ConfigurationStore _configurationStore;
    private Timer _poller;
    private EppoClientConfig _eppoClientConfig;

    private EppoClient(ConfigurationStore configurationStore, Timer poller, EppoClientConfig eppoClientConfig)
    {
        this._configurationStore = configurationStore;
        this._poller = poller;
        this._eppoClientConfig = eppoClientConfig;
    }

    public static EppoClient Init(EppoClientConfig eppoClientConfig)
    {
        lock(Instance)
        {
            InputValidator.validateNotBlank(eppoClientConfig.apiKey,
                                            "An API key is required");
            if (eppoClientConfig.assignmentLogger == null) {
                throw new InvalidDataException("An assignment logging implementation is required");
            }

            AppDetails appDetails = AppDetails.GetInstance();
            EppoHttpClient eppoHttpClient = new EppoHttpClient(
                eppoClientConfig.apiKey,
                appDetails.GetName(),
                appDetails.GetVersion(),
                eppoClientConfig.baseUrl,
                Constants.REQUEST_TIMEOUT_MILLIS
            );

            ExperimentConfigurationRequester expConfigRequester = new ExperimentConfigurationRequester(eppoHttpClient);
            CacheHelper cacheHelper = new CacheHelper();
            Cache<String, ExperimentConfiguration> experimentConfigurationCache = cacheHelper
                .CreateExperimentConfigurationCache(Constants.MAX_CACHE_ENTRIES);
            //TODO: Cache initialization

            ConfigurationStore configurationStore = ConfigurationStore.Init(
                experimentConfigurationCache,
                expConfigRequester
            );

            if (Instance != null) {
                Instance._poller.cancel();
            }

            Timer poller = new(state =>
            {
                configurationStore.FetchExperimentConfiguration();
            });

            Instance = new EppoClient(configurationStore, poller, eppoClientConfig);
        }

        return Instance;
    }

    public static EppoClient GetInstance()
    {
        if (Instance == null)
        {
            throw new EppoClientIsNotInitializedException("Eppo client is not initiased");
        }
        return Instance;
    }
}