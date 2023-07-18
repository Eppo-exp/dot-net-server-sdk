using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.exception;
using eppo_sdk.helpers;
using eppo_sdk.http;
using eppo_sdk.store;
using eppo_sdk.tasks;
using eppo_sdk.validators;
using NLog;

namespace eppo_sdk;

public class EppoClient
{
    private static readonly object Baton = new();

    private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    private static EppoClient? Client = null;
    private ConfigurationStore _configurationStore;
    private FetchExperimentsTask _fetchExperimentsTask;
    private EppoClientConfig _eppoClientConfig;

    private EppoClient(ConfigurationStore configurationStore, EppoClientConfig eppoClientConfig,
        FetchExperimentsTask fetchExperimentsTask)
    {
        this._configurationStore = configurationStore;
        this._eppoClientConfig = eppoClientConfig;
        _fetchExperimentsTask = fetchExperimentsTask;
    }

    public string? GetAssignment(string subjectKey, string flagKey, SubjectAttributes subjectAttributes)
    {
        InputValidator.ValidateNotBlank(subjectKey, "Invalid argument: subjectKey cannot be blank");
        InputValidator.ValidateNotBlank(flagKey, "Invalid argument: flagKey cannot be blank");
        var configuration = this._configurationStore.GetExperimentConfiguration(flagKey);
        if (configuration == null)
        {
            logger.Warn($"[Eppo SDK] No configuration found for key: {flagKey}");
            return null;
        }

        var subjectVariationOverride = this.GetSubjectVariationOverride(subjectKey, configuration);
        if (subjectVariationOverride.value != null)
        {
            return subjectVariationOverride.value;
        }

        if (!configuration.enabled)
        {
            logger.Info(
                $"[Eppo SDK] No assigned variation because the experiment or feature flag {flagKey} is disabled");
            return null;
        }

        var rule = RuleValidator.FindMatchingRule(subjectAttributes, configuration.rules);
        if (rule == null)
        {
            logger.Info("[Eppo SDK] No assigned variation. The subject attributes did not match any targeting rules");
            return null;
        }

        var allocation = configuration.GetAllocation(rule.allocationKey);
        if (!this.IsInExperimentSample(subjectKey, flagKey, configuration.subjectShards, allocation!.percentExposure))
        {
            logger.Info("[Eppo SDK] No assigned variation. The subject is not part of the sample population");
            return null;
        }

        var assignedVariation =
            this.GetAssignedVariation(subjectKey, flagKey, configuration.subjectShards, allocation.variations);
        try
        {
            this._eppoClientConfig.AssignmentLogger
                .LogAssignment(new AssignmentLogData(
                    flagKey,
                    assignedVariation.value.StringValue(),
                    subjectKey,
                    subjectAttributes
                ));
        }
        catch (Exception e)
        {
            // Ignore Exception
        }

        return assignedVariation.value.StringValue();
    }

    public string? GetAssignment(string subjectKey, string experimentKey)
    {
        return this.GetAssignment(subjectKey, experimentKey, new SubjectAttributes());
    }

    private bool IsInExperimentSample(string subjectKey, string experimentKey, int subjectShards,
        float percentageExposure)
    {
        var shard = Shard.GetShard($"exposure-{subjectKey}-{experimentKey}", subjectShards);
        return shard <= percentageExposure * subjectShards;
    }

    private Variation GetAssignedVariation(string subjectKey, string experimentKey, int subjectShards,
        List<Variation> variations)
    {
        var shard = Shard.GetShard($"assignment-{subjectKey}-{experimentKey}", subjectShards);
        return variations.Find(config => Shard.IsInRange(shard, config.shardRange))!;
    }

    public EppoValue GetSubjectVariationOverride(string subjectKey, ExperimentConfiguration experimentConfiguration)
    {
        var hexedSubjectKey = Shard.GetHex(subjectKey);
        return experimentConfiguration.overrides.GetValueOrDefault(hexedSubjectKey, new EppoValue());
    }

    public static EppoClient Init(EppoClientConfig eppoClientConfig)
    {
        lock (Baton)
        {
            InputValidator.ValidateNotBlank(eppoClientConfig.ApiKey,
                "An API key is required");
            if (eppoClientConfig.AssignmentLogger == null)
            {
                throw new InvalidDataException("An assignment logging implementation is required");
            }

            var appDetails = AppDetails.GetInstance();
            var eppoHttpClient = new EppoHttpClient(
                eppoClientConfig.ApiKey,
                appDetails.GetName(),
                appDetails.GetVersion(),
                eppoClientConfig.BaseUrl,
                Constants.REQUEST_TIMEOUT_MILLIS
            );

            var expConfigRequester = new ExperimentConfigurationRequester(eppoHttpClient);
            var cacheHelper = new CacheHelper(Constants.MAX_CACHE_ENTRIES);
            var configurationStore = ConfigurationStore.GetInstance(
                cacheHelper.Cache,
                expConfigRequester
            );

            if (EppoClient.Client != null)
            {
                Client._fetchExperimentsTask.Dispose();
            }

            var fetchExperimentsTask = new FetchExperimentsTask(configurationStore, Constants.TIME_INTERVAL_IN_MILLIS,
                Constants.JITTER_INTERVAL_IN_MILLIS);
            Client = new EppoClient(configurationStore, eppoClientConfig, fetchExperimentsTask);
        }

        return Client;
    }

    public static EppoClient GetInstance()
    {
        if (Client == null)
        {
            throw new EppoClientIsNotInitializedException("Eppo client is not initialized");
        }

        return Client;
    }
}