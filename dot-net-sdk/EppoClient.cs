using System.Linq.Expressions;
using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.exception;
using eppo_sdk.helpers;
using eppo_sdk.http;
using eppo_sdk.store;
using eppo_sdk.tasks;
using eppo_sdk.validators;
using Newtonsoft.Json.Linq;
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

    private HasEppoValue _typeCheckedAssignment(string flagKey, string subjectKey, Subject subjectAttributes, EppoValueType expectedValueType, object defaultValue) {
        var result = GetAssignment(flagKey, subjectKey, subjectAttributes);
        var eppoDefaultValue = new HasEppoValue(defaultValue);
        if (HasEppoValue.IsNullValue(result)) return eppoDefaultValue;
        var assignment = result!;
        if (assignment.Type != expectedValueType) {
            Logger.Warn($"[Eppo SDK] Expected type {expectedValueType} does not match parsed type {assignment.Type}");
            return eppoDefaultValue;
        }
        return assignment;
    }
    public JObject GetJsonAssignment(string flagKey, string subjectKey, Subject subjectAttributes, JObject defaultValue)
    {
        return _typeCheckedAssignment(flagKey, subjectKey, subjectAttributes, EppoValueType.JSON, defaultValue).JsonValue();
    }

    public bool GetBoolAssignment(string flagKey, string subjectKey, Subject subjectAttributes, bool defaultValue)
    {
        return _typeCheckedAssignment(flagKey, subjectKey, subjectAttributes, EppoValueType.BOOLEAN, defaultValue).BoolValue();
    }

    public double GetNumericAssignment(string flagKey, string subjectKey, Subject subjectAttributes, double defaultValue)
    {
        return _typeCheckedAssignment(flagKey, subjectKey, subjectAttributes, EppoValueType.NUMERIC, defaultValue).DoubleValue();
    }


    public long GetIntegerAssignment(string flagKey, string subjectKey, Subject subjectAttributes, long defaultValue)
    {
        return _typeCheckedAssignment(flagKey, subjectKey, subjectAttributes, EppoValueType.INTEGER, defaultValue).IntegerValue();
    }


    public string GetStringAssignment(string flagKey, string subjectKey, Subject subjectAttributes, string defaultValue)
    {
        return _typeCheckedAssignment(flagKey, subjectKey, subjectAttributes, EppoValueType.STRING, defaultValue).StringValue();
    }


    private HasEppoValue? GetAssignment(string flagKey, string subjectKey, Subject subjectAttributes)
    {
        InputValidator.ValidateNotBlank(subjectKey, "Invalid argument: subjectKey cannot be blank");
        InputValidator.ValidateNotBlank(flagKey, "Invalid argument: flagKey cannot be blank");

        var configuration = this._configurationStore.GetExperimentConfiguration(flagKey);
        if (configuration == null)
        {
            Logger.Warn($"[Eppo SDK] No configuration found for key: {flagKey}");
            return null;
        }

        if (!configuration.enabled)
        {
            Logger.Info(
                $"[Eppo SDK] No assigned variation because the experiment or feature flag {flagKey} is disabled");
            return null;
        }

        var result = RuleValidator.EvaluateFlag(configuration, subjectKey, subjectAttributes);
        if (result == null)
        {
            Logger.Info("[Eppo SDK] No assigned variation. The subject attributes did not match any targeting rules");
            return null;
        }

        var assignment = result.Variation;

        if (HasEppoValue.IsNullValue(assignment))
        {
            Logger.Warn("[Eppo SDK] Assigned varition is null");
            return null;
        }

        try
        {
            _eppoClientConfig.AssignmentLogger
                .LogAssignment(new AssignmentLogData(
                    flagKey,
                    result.AllocationKey,
                    assignment.StringValue() ?? "null",
                    subjectKey,
                    subjectAttributes
                ));
        }
        catch (Exception)
        {
            // Ignore Exception
        }

        return assignment;
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