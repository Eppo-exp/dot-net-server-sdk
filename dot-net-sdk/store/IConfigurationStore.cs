using eppo_sdk.dto;

namespace eppo_sdk.store;

public interface IConfigurationStore
{
    void FetchExperimentConfiguration();
    ExperimentConfiguration GetExperimentConfiguration(string key);
    void SetExperimentConfiguration(string key, ExperimentConfiguration experimentConfiguration);
}