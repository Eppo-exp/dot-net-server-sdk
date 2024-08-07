using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.store;

namespace eppo_sdk.http;

public interface IConfigurationRequester
{
    void LoadConfiguration();
    bool TryGetBandit(string key, out Bandit? bandit);
    bool TryGetFlag(string key, out Flag? flag);
    public BanditReferences GetBanditReferences();
}

public class ConfigurationRequester : IConfigurationRequester
{
    private const string KEY_BANDIT_REFERENCES = "banditReferences";
    private const string KEY_BANDIT_VERSIONS = "banditVersions";
    private const string KEY_FLAG_CONFIG_VERSION = "ufcVersion";

    private readonly EppoHttpClient eppoHttpClient;
    private readonly IConfigurationStore configurationStore;

    public ConfigurationRequester(EppoHttpClient eppoHttpClient, IConfigurationStore configurationStore)
    {
        this.eppoHttpClient = eppoHttpClient;
        this.configurationStore = configurationStore;
    }

    public bool TryGetBandit(string key, out Bandit? bandit) => configurationStore.TryGetBandit(key, out bandit);

    public bool TryGetFlag(string key, out Flag? flag) => configurationStore.TryGetFlag(key, out flag);

    public BanditReferences GetBanditReferences()
    {
        if (configurationStore.TryGetMetadata(KEY_BANDIT_REFERENCES, out BanditReferences? banditReferences) && banditReferences != null)
        {
            return banditReferences;
        }
        throw new SystemException("Bandit Flag mapping could not be loaded from the cache");
    }

    public void LoadConfiguration()
    {
        // The response from `ConfigurationRequester` is versioned so we can avoid extra work and network bytes
        // by keeping track of the version we have loaded.
        string? lastConfigVersion = GetLastFlagVersion();
        var flagConfigurationResponse = FetchFlags(lastConfigVersion);
        if (flagConfigurationResponse.IsModified)
        {
            // Fetch methods throw if resource is null.
            var flags = flagConfigurationResponse.Resource!;
            var indexer = flags.BanditReferences ?? new BanditReferences();

            var metadata = new Dictionary<string, object>();

            var banditModelList = FetchBanditsIfRequired(indexer);
            if (banditModelList != null)
            {
                // Store the bandits models that are loaded, not just those referenced.
                metadata[KEY_BANDIT_VERSIONS] = banditModelList.Select((bandit) => bandit.ModelVersion);
            }

            var version = flagConfigurationResponse.VersionIdentifier;
            if (version != null)
            {
                metadata[KEY_FLAG_CONFIG_VERSION] = version;
            }
            metadata[KEY_BANDIT_REFERENCES] = indexer;


            configurationStore.SetConfiguration(
                 flags.Flags.ToList().Select(kvp => kvp.Value),
                 banditModelList,
                 metadata);
        }
    }

    /// <summary>
    /// Determine whether to fetch bandits.
    /// </summary>
    /// <param name="indexer"></param>
    /// <returns>Fetched bandits or `null` if fetching was not required.</returns>
    private IEnumerable<Bandit>? FetchBanditsIfRequired(BanditReferences indexer)
    {
        var loadedModels = GetLoadedModels();
        // Only fetch bandit models if there are active references and not all of the referenced models are in the set of loaded models.
        if (indexer.HasBanditReferences() && !indexer.GetBanditModelVersions().All(model => loadedModels.Contains(model)))
        {
            BanditModelResponse banditModels = FetchBandits().Resource!;
            var banditModelList = banditModels.Bandits?.ToList().Select(kvp => kvp.Value) ?? Array.Empty<Bandit>();
            return banditModelList;
        }
        return null;
    }

    private IEnumerable<string> GetLoadedModels()
    {
        configurationStore.TryGetMetadata(KEY_BANDIT_VERSIONS, out IEnumerable<string>? models);
        return models ?? Array.Empty<string>();
    }

    private VersionedResourceResponse<FlagConfigurationResponse> FetchFlags(string? lastConfigVersion)
    {
        try
        {
            var response = eppoHttpClient.Get<FlagConfigurationResponse>(Constants.UFC_ENDPOINT, lastConfigVersion);
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
        configurationStore.TryGetMetadata<string>(KEY_FLAG_CONFIG_VERSION, out string? lastVersion);
        return lastVersion;
    }
}
