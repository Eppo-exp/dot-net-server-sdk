using eppo_sdk.dto;
using eppo_sdk.exception;
using eppo_sdk.http;
using Microsoft.Extensions.Caching.Memory;

namespace eppo_sdk.store;

public class ConfigurationStore : IConfigurationStore
{
    private readonly MemoryCache _experimentConfigurationCache;
    private readonly ExperimentConfigurationRequester _requester;
    private static ConfigurationStore? _instance;

    public ConfigurationStore(ExperimentConfigurationRequester requester, MemoryCache experimentConfigurationCache)
    {
        this._requester = requester;
        this._experimentConfigurationCache = experimentConfigurationCache;
    }

    public static ConfigurationStore GetInstance(MemoryCache experimentConfigurationCache,
        ExperimentConfigurationRequester requester)
    {
        if (_instance == null)
        {
            _instance = new ConfigurationStore(requester, experimentConfigurationCache);
        }
        else
        {
            _instance._experimentConfigurationCache.Clear();
        }

        return _instance;
    }

    public void SetExperimentConfiguration(string key, ExperimentConfiguration experimentConfiguration)
    {
        this._experimentConfigurationCache.Set(key, experimentConfiguration);
    }

    public ExperimentConfiguration? GetExperimentConfiguration(string key)
    {
        try
        {
            if (this._experimentConfigurationCache.TryGetValue(key, out ExperimentConfiguration? result))
            {
                return result;
            }
        }
        catch (Exception)
        {
            throw new ExperimentConfigurationNotFound($"Experiment configuration for key: {key} not found.");
        }

        return null;
    }

    public void FetchExperimentConfiguration()
    {
        ExperimentConfigurationResponse experimentConfigurationResponse = Get();
        experimentConfigurationResponse.flags.ToList()
            .ForEach(x => { this.SetExperimentConfiguration(x.Key, x.Value); });
    }

    private ExperimentConfigurationResponse Get()
    {
        ExperimentConfigurationResponse? response = this._requester.FetchExperimentConfiguration();
        if (response != null)
        {
            return response;
        }

        throw new SystemException("Unable to fetch experiment configuration");
    }
}