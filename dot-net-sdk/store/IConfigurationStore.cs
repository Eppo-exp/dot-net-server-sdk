using eppo_sdk.dto;
using eppo_sdk.dto.bandit;

namespace eppo_sdk.store;

public interface IConfigurationStore
{
    void LoadConfiguration();
    bool TryGetBandit(string key, out Bandit? bandit);
    bool TryGetFlag(string key, out Flag? flag);
    public BanditFlags GetBanditFlags();
}
