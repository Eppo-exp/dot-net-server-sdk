using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
using eppo_sdk.dto.bandit;

namespace eppo_sdk.store;

public interface IConfigurationStore
{
    void LoadConfiguration();
    void SetExperimentConfiguration(string key, Flag experimentConfiguration);
    bool TryGetBandit(string key, out Bandit? bandit);
    bool TryGetFlag(string key, out Flag? flag);
    void SetBanditModel(Bandit bandit);
}