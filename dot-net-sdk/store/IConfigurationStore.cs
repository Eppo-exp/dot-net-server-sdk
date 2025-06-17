using eppo_sdk.dto;
using eppo_sdk.dto.bandit;

namespace eppo_sdk.store;

public interface IConfigurationStore
{
    /// <summary>
    /// Gets the current configuration snapshot containing all flags, bandits, and metadata.
    /// This is the primary method for accessing configuration data.
    /// </summary>
    /// <returns>The current configuration.</returns>
    Configuration GetConfiguration();

    /// <summary>
    /// Sets a new configuration from a Configuration object.
    /// This is the most efficient way to update the configuration.
    /// </summary>
    /// <param name="configuration">The new configuration to set.</param>
    void SetConfiguration(Configuration configuration);
}
