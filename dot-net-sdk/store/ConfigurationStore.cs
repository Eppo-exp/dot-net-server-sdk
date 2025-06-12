using eppo_sdk.dto;
using eppo_sdk.dto.bandit;

namespace eppo_sdk.store;

/// <summary>
/// Thread-safe configuration store that maintains a single Configuration object.
/// Optimized for efficient re-inflation and concurrent read access.
/// </summary>
public class ConfigurationStore : IConfigurationStore
{
    private volatile Configuration _currentConfiguration;
    private readonly object _updateLock = new object();

    /// <summary>
    /// Initializes a new instance of ConfigurationStore with an empty configuration.
    /// </summary>
    public ConfigurationStore()
    {
        _currentConfiguration = Configuration.Empty;
    }

    /// <summary>
    /// Gets the current configuration snapshot.
    /// This is the primary method for accessing configuration data.
    /// </summary>
    /// <returns>The current configuration containing all flags, bandits, and metadata.</returns>
    public Configuration GetConfiguration()
    {
        return _currentConfiguration; // Volatile read, thread-safe
    }

    /// <summary>
    /// Sets a new configuration from a Configuration object.
    /// This is the most efficient way to update the configuration.
    /// </summary>
    /// <param name="configuration">The new configuration to set.</param>
    public void SetConfiguration(Configuration configuration)
    {
        lock (_updateLock)
        {
            _currentConfiguration = configuration;
        }
    }
}
