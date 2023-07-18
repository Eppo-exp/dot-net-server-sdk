using eppo_sdk.constants;
using eppo_sdk.exception;
using eppo_sdk.helpers;
using eppo_sdk.http;
using eppo_sdk.store;
using eppo_sdk.tasks;

namespace eppo_sdk;

public class EppoClient
{
    private static readonly object Baton = new();

    private static EppoClient? Client;
    private ConfigurationStore _configurationStore;
    private FetchExperimentsTask _fetchExperimentsTask;
    private EppoClientConfig _eppoClientConfig;

    private EppoClient(ConfigurationStore configurationStore, EppoClientConfig eppoClientConfig, FetchExperimentsTask fetchExperimentsTask)
    {
        this._configurationStore = configurationStore;
        this._eppoClientConfig = eppoClientConfig;
        _fetchExperimentsTask = fetchExperimentsTask;
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
                Client._fetchExperimentsTask.Dispose();
            }

            var fetchExperimentsTask = new FetchExperimentsTask(configurationStore, Constants.TIME_INTERVAL_IN_MILLIS, Constants.JITTER_INTERVAL_IN_MILLIS);
            Client = new EppoClient(configurationStore, eppoClientConfig, fetchExperimentsTask);
        }

        return Client;
    }

    public static EppoClient GetInstance()
    {
        if (Client == null)
        {
            throw new EppoClientIsNotInitializedException("Eppo client is not initialized");
        }

        return Client;
    }
}