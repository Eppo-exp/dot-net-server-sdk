using eppo_sdk.constants;
using eppo_sdk.exception;
using eppo_sdk.helpers;
using eppo_sdk.http;
using eppo_sdk.store;

namespace eppo_sdk;

public class EppoClient
{
    private static readonly object Baton = new();

    private static EppoClient? Client;
    private ConfigurationStore _configurationStore;
    private readonly Timer _poller;
    private EppoClientConfig _eppoClientConfig;

    private EppoClient(ConfigurationStore configurationStore, Timer poller, EppoClientConfig eppoClientConfig)
    {
        this._configurationStore = configurationStore;
        this._poller = poller;
        this._eppoClientConfig = eppoClientConfig;
    }

    public static EppoClient Init(EppoClientConfig eppoClientConfig)
    {
        lock (Baton)
        {
            InputValidator.ValidateNotBlank(eppoClientConfig.ApiKey,
                "An API key is required");
            if (eppoClientConfig.AssignmentLogger == null)
            {
                throw new InvalidDataException("An assignment logging implementation is required");
            }

            var appDetails = AppDetails.GetInstance();
            var eppoHttpClient = new EppoHttpClient(
                eppoClientConfig.ApiKey,
                appDetails.GetName(),
                appDetails.GetVersion(),
                eppoClientConfig.BaseUrl,
                Constants.REQUEST_TIMEOUT_MILLIS
            );

            var expConfigRequester = new ExperimentConfigurationRequester(eppoHttpClient);
            var cacheHelper = new CacheHelper(Constants.MAX_CACHE_ENTRIES);
            var configurationStore = ConfigurationStore.GetInstance(
                cacheHelper.Cache,
                expConfigRequester
            );

            if (Client != null)
            {
                Client._poller.Dispose();
            }

            Timer poller = new(state => { configurationStore.FetchExperimentConfiguration(); });

            Client = new EppoClient(configurationStore, poller, eppoClientConfig);
        }

        return Client;
    }

    public static EppoClient GetInstance()
    {
        if (Client == null)
        {
            throw new EppoClientIsNotInitializedException("Eppo client is not initiased");
        }

        return Client;
    }
}