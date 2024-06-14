using eppo_sdk.dto;
using eppo_sdk.dto.bandit;

namespace eppo_sdk.store;

public interface IConfigurationStore
{
    void LoadConfiguration();
    Flag? GetExperimentConfiguration(string key);
    void SetExperimentConfiguration(string key, Flag experimentConfiguration);
    bool TryGetBandit(string key, out Bandit? bandit);
    bool TryGetFlag(string key, out Flag? bandit);
    void SetBanditModel(Bandit bandit);
}