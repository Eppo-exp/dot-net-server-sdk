using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.store;

namespace eppo_sdk.http;

public interface IConfigurationRequester
{
    /// <summary>
    /// Gets the current configuration snapshot.
    /// </summary>
    /// <returns>The current configuration containing all flags, bandits, and metadata.</returns>
    Configuration GetConfiguration();

    /// <summary>
    /// Fetches and activates the configuration.
    /// </summary>
    void FetchAndActivateConfiguration();
}

public class ConfigurationRequester : IConfigurationRequester
{
    private readonly EppoHttpClient eppoHttpClient;
    private readonly IConfigurationStore configurationStore;

    public ConfigurationRequester(
        EppoHttpClient eppoHttpClient,
        IConfigurationStore configurationStore
    )
    {
        this.eppoHttpClient = eppoHttpClient;
        this.configurationStore = configurationStore;
    }

    /// <summary>
    /// Gets the current configuration snapshot.
    /// </summary>
    /// <returns>The current configuration containing all flags, bandits, and metadata.</returns>
    public Configuration GetConfiguration()
    {
        return configurationStore.GetConfiguration();
    }

    public void FetchAndActivateConfiguration()
    {
        // The response from `ConfigurationRequester` is versioned so we can avoid extra work and network bytes
        // by keeping track of the version we have loaded.
        string? lastConfigVersion = GetLastFlagVersion();
        var currentConfig = GetConfiguration();
        var flagConfigurationResponse = FetchFlags(lastConfigVersion);

        if (flagConfigurationResponse.IsModified)
        {
            // Fetch methods throw if resource is null.
            var flags = flagConfigurationResponse.Resource!;

            var banditReferences = flags.BanditReferences ?? new BanditReferences();
            var banditModelVersions = banditReferences.GetBanditModelVersions();

            var currentBanditModels = currentConfig.GetBanditModelVersions();

            var shouldFetchBandits = !banditModelVersions.All(model =>
                currentBanditModels.Contains(model)
            );

            if (shouldFetchBandits)
            {
                // Need to fetch new bandits
                var banditResponse = FetchBandits();
                var newConfiguration = new Configuration(flagConfigurationResponse, banditResponse);
                configurationStore.SetConfiguration(newConfiguration);
            }
            else
            {
                // Use existing bandits
                var newConfiguration = currentConfig.WithNewFlags(flagConfigurationResponse);
                configurationStore.SetConfiguration(newConfiguration);
            }
        }
    }

    private VersionedResourceResponse<FlagConfigurationResponse> FetchFlags(
        string? lastConfigVersion
    )
    {
        try
        {
            var response = eppoHttpClient.Get<FlagConfigurationResponse>(
                Constants.UFC_ENDPOINT,
                lastConfigVersion
            );
            if (response.IsModified && response.Resource == null)
            {
                // Invalid and unexpected state.
                throw new SystemException("Flag configuration not present in response");
            }
            return response;
        }
        catch (Exception e)
        {
            throw new SystemException("Unable to fetch flag configuration" + e.Message);
        }
    }

    private VersionedResourceResponse<BanditModelResponse> FetchBandits()
    {
        try
        {
            var response = eppoHttpClient.Get<BanditModelResponse>(Constants.BANDIT_ENDPOINT);
            if (response.Resource == null)
            {
                throw new SystemException("Bandit configuration not present in response");
            }
            return response;
        }
        catch (Exception e)
        {
            throw new SystemException("Unable to fetch bandit configuration" + e.Message);
        }
    }

    private string? GetLastFlagVersion()
    {
        var configuration = GetConfiguration();
        return configuration.GetFlagConfigVersion();
    }
}
