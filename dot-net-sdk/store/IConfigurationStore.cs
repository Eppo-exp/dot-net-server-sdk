using eppo_sdk.dto;
using eppo_sdk.dto.bandit;

namespace eppo_sdk.store;

public interface IConfigurationStore
{
    void FetchConfiguration();
    Flag? GetExperimentConfiguration(string key);
    void SetExperimentConfiguration(string key, Flag experimentConfiguration);
    Bandit? GetBanditModel(string key);
    void SetBanditModel(Bandit bandit);

    public BanditFlags GetBanditFlags();
    public void SetBanditFlags(BanditFlags banditFlags);
}
