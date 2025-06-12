using eppo_sdk.dto;
using eppo_sdk.dto.bandit;

namespace eppo_sdk.store;

public interface IConfigurationStore
{
    /// <summary>
    /// Sets all configuration values (UFC, metadata, and Bandits) in one idempotent method.
    ///
    /// Since Bandits can optionally be loaded from the API, see the overload to set only the UFC configuration.
    /// </summary>
    /// <param name="flags"></param>
    /// <param name="banditFlagReferences"></param>
    /// <param name="bandits">Bandit models to set. If null, existing bandits are not overwritten.</param>
    /// <param name="metadata"></param>
    void SetConfiguration(
        IEnumerable<Flag> flags,
        IEnumerable<Bandit> bandits,
        IDictionary<string, object> metadata
    );

    /// <summary>
    /// Sets just the UFC configuration values (flags and metadata), **without updating `bandits`**, in one idempotent method.
    /// </summary>
    /// <param name="flags"></param>
    /// <param name="banditFlagReferences"></param>
    /// <param name="bandits">Bandit models to set. If null, existing bandits are not overwritten.</param>
    /// <param name="metadata"></param>
    void SetConfiguration(IEnumerable<Flag> flags, IDictionary<string, object> metadata);

    /// <summary>
    /// Gets the current configuration snapshot containing all flags, bandits, and metadata.
    /// </summary>
    /// <returns>A Configuration object representing the current state.</returns>
    Configuration GetConfiguration();

    bool TryGetBandit(string key, out Bandit? bandit);
    bool TryGetFlag(string key, out Flag? flag);
    bool TryGetMetadata<MType>(string key, out MType? metadata);
}
