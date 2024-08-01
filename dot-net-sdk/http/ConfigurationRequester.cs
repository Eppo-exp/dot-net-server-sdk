using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.store;
using NLog;

namespace eppo_sdk.http;

public interface IConfigurationRequester
{
    void LoadConfiguration();
    bool TryGetBandit(string key, out Bandit? bandit);
    bool TryGetFlag(string key, out Flag? flag);
    public BanditFlags GetBanditFlags();
}

public class ConfigurationRequester : IConfigurationRequester
{
    private const string BANDIT_FLAGS_KEY = "banditFlags";
    private const string KEY_FLAG_CONFIG_VERSION = "ufcVersion";

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private readonly EppoHttpClient eppoHttpClient;
    private readonly IConfigurationStore configurationStore;

    public ConfigurationRequester(EppoHttpClient eppoHttpClient, IConfigurationStore configurationStore)
    {
        this.eppoHttpClient = eppoHttpClient;
        this.configurationStore = configurationStore;
    }

    public bool TryGetBandit(string key, out Bandit? bandit) => configurationStore.TryGetBandit(key, out bandit);

    public bool TryGetFlag(string key, out Flag? flag) => configurationStore.TryGetFlag(key, out flag);

    public BanditFlags GetBanditFlags()
    {
        if (configurationStore.TryGetMetadata(BANDIT_FLAGS_KEY, out BanditFlags? banditFlags) && banditFlags != null)
        {
            return banditFlags;
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
            var banditFlags = flags.Bandits ?? new BanditFlags();

            IEnumerable<Bandit> banditModelList = Array.Empty<Bandit>();
            if (banditFlags.Count > 0)
            {
                BanditModelResponse banditModels = FetchBandits().Resource!;
                banditModelList = banditModels.Bandits?.ToList().Select(kvp => kvp.Value) ?? Array.Empty<Bandit>();
            }

            var metadata = new Dictionary<string, object>();
            var version = flagConfigurationResponse.VersionIdentifier;
            if (version != null)
            {
                metadata[KEY_FLAG_CONFIG_VERSION] = version;
            }
            metadata[BANDIT_FLAGS_KEY] = banditFlags;

            configurationStore.SetConfiguration(
                 flags.Flags.ToList().Select(kvp => kvp.Value),
                 banditModelList,
                 metadata);
        }
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
