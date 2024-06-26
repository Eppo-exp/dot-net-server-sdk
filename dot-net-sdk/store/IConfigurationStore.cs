using eppo_sdk.dto;
using eppo_sdk.dto.bandit;

namespace eppo_sdk.store;

public interface IConfigurationStore
{
    void LoadConfiguration();
    void SetFlag(string key, Flag flag);
    bool TryGetBandit(string key, out Bandit? bandit);
    bool TryGetFlag(string key, out Flag? flag);
    void SetBanditModel(Bandit bandit);

    public BanditFlags GetBanditFlags();
    public void SetBanditFlags(BanditFlags banditFlags);
}
