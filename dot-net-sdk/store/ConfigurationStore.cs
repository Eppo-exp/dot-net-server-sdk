using dot_net_eppo.dto;
using dot_net_sdk.exception;
using dot_net_sdk.http;

namespace dot_net_sdk.store;

public class ConfigurationStore
{
    // TODO: Change to cache 
    private int experimentConfigurationCache;
    private ExperimentConfigurationRequester requester;
    private static ConfigurationStore Instance;

    public ConfigurationStore(ExperimentConfigurationRequester requester, int experimentConfigurationCache)
    {
        this.requester = requester;
        this.experimentConfigurationCache = experimentConfigurationCache;
    }

    public static ConfigurationStore Init(int experimentConfigurationCache, ExperimentConfigurationRequester requester)
    {
        if (Instance == null)
        {
            Instance = new ConfigurationStore(requester, experimentConfigurationCache);
        }

        Instance.experimentConfigurationCache.clear();
        return Instance;
    }

    public static ConfigurationStore GetInstance()
    {
        return Instance;
    }

    public void SetExperimentConfiguration(String key, ExperimentConfiguration experimentConfiguration)
    {
        // TODO: this.experimentConfiguration.put(key, experimentConfiguration);
    }

    public ExperimentConfiguration GetExperimentConfiguration(String key)
    {
        try
        {
            // TODO: Read from cache
            // return this.experimentConfigurationCache.get(key);
            return null;
        }
        catch (Exception e)
        {
            throw new ExperimentConfigurationNotFound($"Experiment configuration for key: {key} not found.");
        }
    }

    public void FetchExperimentConfiguration()
    {
        ExperimentConfigurationResponse experimentConfigurationResponse = Get();
        experimentConfigurationResponse.flags.ToList().ForEach(x =>
        {
            this.SetExperimentConfiguration(x.Key, x.Value);
        });
    }

    private ExperimentConfigurationResponse Get()
    {
        ExperimentConfigurationResponse? response = this.requester.FetchExperimentConfiguration();
        if (response != null)
        {
            return response;
        }
    }
}