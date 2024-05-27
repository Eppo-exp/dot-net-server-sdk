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

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static EppoClient? _client = null;
    private readonly ConfigurationStore _configurationStore;
    private readonly FetchExperimentsTask _fetchExperimentsTask;
    private readonly EppoClientConfig _eppoClientConfig;

    private EppoClient(ConfigurationStore configurationStore, EppoClientConfig eppoClientConfig,
        FetchExperimentsTask fetchExperimentsTask)
    {
        _configurationStore = configurationStore;
        _eppoClientConfig = eppoClientConfig;
        _fetchExperimentsTask = fetchExperimentsTask;
    }

    public bool? GetBoolAssignment(string subjectKey, string flagKey, SubjectAttributes? subjectAttributes = null)
    {
        return GetAssignment(subjectKey, flagKey, subjectAttributes ?? new SubjectAttributes())?.BoolValue();
    }

    public double? GetNumericAssignment(string subjectKey, string flagKey, SubjectAttributes? subjectAttributes = null)
    {
        return GetAssignment(subjectKey, flagKey, subjectAttributes ?? new SubjectAttributes())?.DoubleValue();
    }


    public string? GetStringAssignment(string subjectKey, string flagKey, SubjectAttributes? subjectAttributes = null)
    {
        return GetAssignment(subjectKey, flagKey, subjectAttributes ?? new SubjectAttributes())?.StringValue();
    }


    private EppoValue? GetAssignment(string subjectKey, string flagKey, SubjectAttributes subjectAttributes)
    {
        InputValidator.ValidateNotBlank(subjectKey, "Invalid argument: subjectKey cannot be blank");
        InputValidator.ValidateNotBlank(flagKey, "Invalid argument: flagKey cannot be blank");

        var configuration = this._configurationStore.GetExperimentConfiguration(flagKey);
        if (configuration == null)
        {
            Logger.Warn($"[Eppo SDK] No configuration found for key: {flagKey}");
            return null;
        }

        var subjectVariationOverride = this.GetSubjectVariationOverride(subjectKey, configuration);
        if (!subjectVariationOverride.isNull())
        {
            return subjectVariationOverride;
        }

        if (!configuration.enabled)
        {
            Logger.Info(
                $"[Eppo SDK] No assigned variation because the experiment or feature flag {flagKey} is disabled");
            return null;
        }

        var rule = RuleValidator.FindMatchingRule(subjectAttributes, configuration.rules);
        if (rule == null)
        {
            Logger.Info("[Eppo SDK] No assigned variation. The subject attributes did not match any targeting rules");
            return null;
        }

        var allocation = configuration.GetAllocation(rule.allocationKey);
        if (!IsInExperimentSample(subjectKey, flagKey, configuration.subjectShards, allocation!.percentExposure))
        {
            Logger.Info("[Eppo SDK] No assigned variation. The subject is not part of the sample population");
            return null;
        }

        var assignedVariation =
            GetAssignedVariation(subjectKey, flagKey, configuration.subjectShards, allocation.variations);
        try
        {
            _eppoClientConfig.AssignmentLogger
                .LogAssignment(new AssignmentLogData(
                    flagKey,
                    rule.allocationKey,
                    assignedVariation.typedValue.StringValue(),
                    subjectKey,
                    subjectAttributes
                ));
        }
        catch (Exception)
        {
            // Ignore Exception
        }

        return assignedVariation?.typedValue;
    }

    private bool IsInExperimentSample(string subjectKey, string flagKey, int subjectShards,
        float percentageExposure)
    {
        var shard = Sharder.GetShard($"exposure-{subjectKey}-{flagKey}", subjectShards);
        return shard <= percentageExposure * subjectShards;
    }

    private Variation GetAssignedVariation(string subjectKey, string flagKey, int subjectShards,
        List<Variation> variations)
    {
        var shard = Sharder.GetShard($"assignment-{subjectKey}-{flagKey}", subjectShards);
        return variations.Find(config => Sharder.IsInRange(shard, config.shardRange))!;
    }

    public EppoValue GetSubjectVariationOverride(string subjectKey, ExperimentConfiguration experimentConfiguration)
    {
        var hexedSubjectKey = Sharder.GetHex(subjectKey);
        return experimentConfiguration.typedOverrides.GetValueOrDefault(hexedSubjectKey, new EppoValue());
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

            if (_client != null)
            {
                _client._fetchExperimentsTask.Dispose();
            }

            var fetchExperimentsTask = new FetchExperimentsTask(configurationStore, Constants.TIME_INTERVAL_IN_MILLIS,
                Constants.JITTER_INTERVAL_IN_MILLIS);
            fetchExperimentsTask.Run();
            _client = new EppoClient(configurationStore, eppoClientConfig, fetchExperimentsTask);
        }

        return _client;
    }

    public static EppoClient GetInstance()
    {
        if (_client == null)
        {
            throw new EppoClientIsNotInitializedException("Eppo client is not initialized");
        }

        return _client;
    }
}