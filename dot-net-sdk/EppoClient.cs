using eppo_sdk.constants;
using eppo_sdk.dto;
using eppo_sdk.dto.bandit;
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
    private readonly IConfigurationStore _configurationStore;
    private readonly FetchExperimentsTask _fetchExperimentsTask;
    private readonly BanditEvaluator _banditEvaluator;
    private readonly EppoClientConfig _eppoClientConfig;

    private EppoClient(IConfigurationStore configurationStore, EppoClientConfig eppoClientConfig,
        FetchExperimentsTask fetchExperimentsTask)
    {
        _configurationStore = configurationStore;
        _eppoClientConfig = eppoClientConfig;
        _fetchExperimentsTask = fetchExperimentsTask;
        _banditEvaluator = new BanditEvaluator();
    }

    private HasEppoValue _typeCheckedAssignment(string flagKey,
                                                string subjectKey,
                                                IDictionary<string, object> subjectAttributes,
                                                EppoValueType expectedValueType,
                                                object defaultValue)
    {
        var result = GetAssignment(flagKey, subjectKey, subjectAttributes);
        var eppoDefaultValue = new HasEppoValue(defaultValue);
        if (HasEppoValue.IsNullValue(result)) return eppoDefaultValue;
        var assignment = result!;
        if (assignment.Type != expectedValueType)
        {
            Logger.Warn($"[Eppo SDK] Expected type {expectedValueType} does not match parsed type {assignment.Type}");
            return eppoDefaultValue;
        }
        return assignment;
    }

    public JObject GetJsonAssignment(string flagKey,
                                     string subjectKey,
                                     IDictionary<string, object> subjectAttributes,
                                     JObject defaultValue)
    {
        return _typeCheckedAssignment(
            flagKey,
            subjectKey,
            subjectAttributes,
            EppoValueType.JSON,
            defaultValue).JsonValue();
    }

    public string GetJsonStringAssignment(string flagKey,
                                          string subjectKey,
                                          IDictionary<string, object> subjectAttributes,
                                          string defaultValue)
    {
        return _typeCheckedAssignment(
            flagKey,
            subjectKey,
            subjectAttributes,
            EppoValueType.JSON,
            defaultValue).StringValue();
    }

    public bool GetBooleanAssignment(string flagKey,
                                     string subjectKey,
                                     IDictionary<string, object> subjectAttributes,
                                     bool defaultValue)
    {
        return _typeCheckedAssignment(
            flagKey,
            subjectKey,
            subjectAttributes,
            EppoValueType.BOOLEAN,
            defaultValue).BoolValue();
    }

    public double GetNumericAssignment(string flagKey,
                                       string subjectKey,
                                       IDictionary<string, object> subjectAttributes,
                                       double defaultValue)
    {
        return _typeCheckedAssignment(
            flagKey,
            subjectKey,
            subjectAttributes,
            EppoValueType.NUMERIC,
            defaultValue).DoubleValue();
    }

    public long GetIntegerAssignment(string flagKey,
                                     string subjectKey,
                                     IDictionary<string, object> subjectAttributes,
                                     long defaultValue)
    {
        return _typeCheckedAssignment(
            flagKey,
            subjectKey,
            subjectAttributes,
            EppoValueType.INTEGER,
            defaultValue).IntegerValue();
    }

    public string GetStringAssignment(string flagKey,
                                      string subjectKey,
                                      IDictionary<string, object> subjectAttributes,
                                      string defaultValue)
    {
        return _typeCheckedAssignment(
            flagKey,
            subjectKey,
            subjectAttributes,
            EppoValueType.STRING,
            defaultValue).StringValue();
    }


    private HasEppoValue? GetAssignment(string flagKey,
                                        string subjectKey,
                                        IDictionary<string, object> subjectAttributes)
    {
        InputValidator.ValidateNotBlank(subjectKey, "Invalid argument: subjectKey cannot be blank");
        InputValidator.ValidateNotBlank(flagKey, "Invalid argument: flagKey cannot be blank");

        if (!_configurationStore.TryGetFlag(flagKey, out Flag? configuration) || configuration == null)
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

        AssignmentLogData assignmentEvent = new AssignmentLogData(
                    flagKey,
                    result.AllocationKey,
                    result.Variation.Key,
                    subjectKey,
                    subjectAttributes.AsReadOnly(),
                    AppDetails.GetInstance().AsDict(),
                    result.ExtraLogging
                );

        if (result.DoLog)
        {
            try
            {
                _eppoClientConfig.AssignmentLogger
                    .LogAssignment(assignmentEvent);
            }
            catch (Exception)
            {
                // Ignore Exception
            }
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

            var expConfigRequester = new ConfigurationRequester(eppoHttpClient);
            var configCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
            var modelCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
            var configurationStore = new ConfigurationStore(
                expConfigRequester,
                configCache,
                modelCache
            );

            _client?._fetchExperimentsTask.Dispose();

            var fetchExperimentsTask = new FetchExperimentsTask(configurationStore, Constants.TIME_INTERVAL_IN_MILLIS,
                Constants.JITTER_INTERVAL_IN_MILLIS);
            fetchExperimentsTask.Run();
            _client = new EppoClient(configurationStore, eppoClientConfig, fetchExperimentsTask);
        }

        return _client;
    }


    /// <summary>Gets the selected action, if applicable, for the given <paramref name="flagKey"/> and contexts.
    /// </summary>
    /// <param name="flagKey">The flag or bandit key to lookup.</param>
    /// <param name="subject">The subject's identifier and a collection of attributes.</param>
    /// <param name="actions">The actions to consider and their contextual attributes.</param>
    /// <param name="defaultValue">Default flag variation.</param>
    public BanditResult GetBanditAction(string flagKey,
                                        ContextAttributes subject,
                                        IDictionary<string, ContextAttributes> actions,
                                        string defaultValue)
    {
        try
        {
            return _getBanditDetail(flagKey, subject, actions, defaultValue);
        }
        catch (Exception e)
        {
            Logger.Error("[Eppo SDK] error getting Bandit action: " + e.Message);
            return new BanditResult(defaultValue);
        }

    }

    /// <summary>Gets the selected action, if applicable, for the given <paramref name="flagKey"/> and contexts.
    /// </summary>
    /// <param name="flagKey">The flag or bandit key to lookup.</param>
    /// <param name="subjectKey">The subject's identifier.</param>
    /// <param name="subjectAttributes">The subject's attributes for consideration
    /// <para>Note: Attributes are sorted based on type into Categorical (String, boolean) and 
    /// Numerical attributes (numbers). All other attributes are discarded and a warning is logged.</para></param>
    /// <param name="actions">The actions to consider and their contextual attributes.
    /// <para>Note: Attributes are sorted based on type into Categorical (String, boolean) and 
    /// Numerical attributes (numbers). All other attributes are discarded and a warning is logged.</para></param>
    /// <param name="defaultValue">Default flag variation.</param>
    public BanditResult GetBanditAction(string flagKey,
                                        string subjectKey,
                                        IDictionary<string, object?> subjectAttributes,
                                        IDictionary<string, IDictionary<string, object?>> actions,
                                        string defaultValue)
    {
        return GetBanditAction(
            flagKey,
            new ContextAttributes(subjectKey, subjectAttributes),
            actions.ToDictionary(kvp => kvp.Key, kvp => new ContextAttributes(kvp.Key, kvp.Value)),
            defaultValue);
    }


    private BanditResult _getBanditDetail(string flagKey,
                                          ContextAttributes subject,
                                          IDictionary<string, ContextAttributes> actions,
                                          string defaultValue)
    {
        // Get the user's flag assignment for the given key.
        var variation = GetStringAssignment(
            flagKey,
            subject.Key,
            subject,
            defaultValue);


        try
        {
            if (_configurationStore.TryGetBandit(variation, out Bandit? bandit) && bandit != null)
            {
                var result = _banditEvaluator.EvaluateBandit(
                    flagKey,
                    subject,
                    actions,
                    bandit.ModelData);

                var banditActionLog = new BanditLogEvent(
                    variation,
                    result,
                    bandit,
                    AppDetails.GetInstance().AsDict());
                _eppoClientConfig.AssignmentLogger.LogBanditAction(banditActionLog);
                return new BanditResult(variation, result.ActionKey);
            }
            else
            {
                Logger.Info($"[Eppo SDK] Variation {variation} is not a valid bandit");
            }
        }
        catch (BanditEvaluationException bee)
        {
            Logger.Error("[Eppo SDK] Error evaluating bandit, returning variation only: " + bee.Message);
        }

        return new(variation);

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
