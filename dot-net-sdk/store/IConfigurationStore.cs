using eppo_sdk.dto;
using eppo_sdk.dto.bandit;

namespace eppo_sdk.store;

public interface IConfigurationStore
{
    /// <summary>
    /// Sets all configuration values in one method to make use of read/write locks.
    /// Pass `null` for `bandits` to preserve existing 
    /// </summary>
    /// <param name="flags"></param>
    /// <param name="banditFlagReferences"></param>
    /// <param name="bandits">Bandit models to set. If null, existing bandits are not overwritten.</param>
    /// <param name="metadata"></param>
    void SetConfiguration(IEnumerable<Flag> flags, IEnumerable<Bandit>? bandits, IDictionary<string, object> metadata);
    bool TryGetBandit(string key, out Bandit? bandit);
    bool TryGetFlag(string key, out Flag? flag);
    bool TryGetMetadata<MType>(string key, out MType? metadata);
}
