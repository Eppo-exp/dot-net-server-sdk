using eppo_sdk.dto;

namespace eppo_sdk.store;

public interface IConfigurationStore
{
    void FetchExperimentConfiguration();
    Flag? GetExperimentConfiguration(string key);
    void SetExperimentConfiguration(string key, Flag experimentConfiguration);
}