using eppo_sdk.client;
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

public sealed class EppoClient: IDisposable
{
    private static readonly object s_baton = new();

    private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();

    private static EppoClient? s_client = null;
    private readonly IConfigurationRequester _config;
    private readonly FetchExperimentsTask? _fetchExperimentsTask;
    private readonly BanditEvaluator _banditEvaluator;
    private readonly EppoClientConfig _eppoClientConfig;

    private readonly AppDetails _appDetails;

    public JObject GetJsonAssignment(string flagKey,
                                     string subjectKey,
                                     IDictionary<string, object> subjectAttributes,
                                     JObject defaultValue)
    {
        return TypeCheckedAssignment(
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
        return TypeCheckedAssignment(
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
        return TypeCheckedAssignment(
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
        return TypeCheckedAssignment(
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
        return TypeCheckedAssignment(
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
        return TypeCheckedAssignment(
            flagKey,
            subjectKey,
            subjectAttributes,
            EppoValueType.STRING,
            defaultValue).StringValue();
    }


    private EppoClient(IConfigurationRequester configurationStore,
                       EppoClientConfig eppoClientConfig,
                       FetchExperimentsTask? fetchExperimentsTask,
                       AppDetails appDetails)
    {
        _config = configurationStore;
        _eppoClientConfig = eppoClientConfig;
        _fetchExperimentsTask = fetchExperimentsTask;
        _banditEvaluator = new BanditEvaluator();
        _appDetails = appDetails;
    }

    private HasEppoValue TypeCheckedAssignment(string flagKey,
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
            s_logger.Warn($"[Eppo SDK] Expected type {expectedValueType} does not match parsed type {assignment.Type}");
            return eppoDefaultValue;
        }
        return assignment;
    }

    private HasEppoValue? GetAssignment(string flagKey,
                                        string subjectKey,
                                        IDictionary<string, object> subjectAttributes)
    {
        InputValidator.ValidateNotBlank(subjectKey, "Invalid argument: subjectKey cannot be blank");
        InputValidator.ValidateNotBlank(flagKey, "Invalid argument: flagKey cannot be blank");

        if (!_config.TryGetFlag(flagKey, out Flag? configuration) || configuration == null)
        {
            s_logger.Warn($"[Eppo SDK] No configuration found for key: {flagKey}");
            return null;
        }

        if (!configuration.Enabled)
        {
            s_logger.Info(
                $"[Eppo SDK] No assigned variation because the experiment or feature flag {flagKey} is disabled");
            return null;
        }

        var result = RuleValidator.EvaluateFlag(configuration, subjectKey, subjectAttributes);
        if (result == null)
        {
            s_logger.Info("[Eppo SDK] No assigned variation. The subject attributes did not match any targeting rules");
            return null;
        }

        var assignment = result.Variation;

        if (HasEppoValue.IsNullValue(assignment))
        {
            s_logger.Warn("[Eppo SDK] Assigned varition is null");
            return null;
        }

        AssignmentLogData assignmentEvent = new AssignmentLogData(
                    flagKey,
                    result.AllocationKey,
                    result.Variation.Key,
                    subjectKey,
                    subjectAttributes.AsReadOnly(),
                    _appDetails.AsDict(),
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

    public static EppoClient InitClientMode(EppoClientConfig eppoClientConfig) =>
        InitInner(
            eppoClientConfig,
            DeploymentEnvironment.Client());


    public static EppoClient Init(EppoClientConfig eppoClientConfig) =>
        InitInner(
            eppoClientConfig,
            DeploymentEnvironment.Server());

    private static EppoClient InitInner(EppoClientConfig eppoClientConfig,
                                        DeploymentEnvironment sdkDeploymentMode)
    {
        lock (s_baton)
        {
            InputValidator.ValidateNotBlank(eppoClientConfig.ApiKey,
                "An API key is required");
            if (eppoClientConfig.AssignmentLogger == null)
            {
                throw new InvalidDataException("An assignment logging implementation is required");
            }

            var appDetails = new AppDetails(sdkDeploymentMode);

            var eppoHttpClient = new EppoHttpClient(
                eppoClientConfig.ApiKey,
                appDetails.Name,
                appDetails.Version,
                eppoClientConfig.BaseUrl,
                Constants.REQUEST_TIMEOUT_MILLIS
            );

            var configCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
            var modelCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;
            var banditFlagCache = new CacheHelper(Constants.MAX_CACHE_ENTRIES).Cache;

            var configurationStore = new ConfigurationStore(
                configCache,
                modelCache,
                banditFlagCache);

            var configRequester = new ConfigurationRequester(eppoHttpClient, configurationStore);
            s_client?._fetchExperimentsTask?.Dispose();

            FetchExperimentsTask? fetchExperimentsTask = null;
            if (appDetails.Deployment.Polling)
            {
                fetchExperimentsTask = new FetchExperimentsTask(
                    configRequester,
                    eppoClientConfig.PollingIntervalInMillis,
                    eppoClientConfig.PollingJitterInMillis);
            }

            // Load the configuration for the first time.
            configRequester.LoadConfiguration();

            s_client = new EppoClient(
                configRequester,
                eppoClientConfig,
                fetchExperimentsTask,
                appDetails);
        }

        return s_client;
    }


    /// <summary>Selects a Bandit Action from the list of <paramref name="actions"/>, if applicable, for the given <paramref name="flagKey"/> and subject context.
    /// <param name="flagKey">The flag or bandit key to lookup.</param>
    /// <param name="subject">The subject's identifier and a collection of attributes.</param>
    /// <param name="actions">A list of actions without contextual attributes.</param>
    /// <param name="defaultValue">Default flag variation.</param>
    /// <example>
    /// For Example:
    /// <code>
    /// var client = EppoClient.GetInstance();
    /// var subject = new ContextAttributes("subjectKey")
    /// {
    ///     ["age"] = 30,
    ///     ["country"] = "uk",
    ///     ["pricingTier"] = "1"  // NOTE: Deliberately setting to string causes this to be treated as a categorical attribute
    /// };
    ///
    /// var actions = new string[] { "adidas", "nike", "Reebok" };
    ///
    /// var result = client.GetBanditAction(
    ///     "flagKey",
    ///     subject,
    ///     actions,
    ///     "defaultValue"
    /// );
    /// </code></example>
    /// </summary>
    public BanditResult GetBanditAction(string flagKey,
                                        ContextAttributes subject,
                                        string[] actions,
                                        string defaultValue)
    {
        try
        {
            var actionContexts = actions.Distinct().ToDictionary(action => action, action => new ContextAttributes(action));
            return GetBanditDetail(flagKey, subject, actionContexts, defaultValue);
        }
        catch (Exception e)
        {
            s_logger.Error("[Eppo SDK] error getting Bandit action: " + e.Message);
            return new BanditResult(defaultValue);
        }

    }


    /// <summary>Gets the selected action, if applicable, for the given <paramref name="flagKey"/> and contexts.
    /// <param name="flagKey">The flag or bandit key to lookup.</param>
    /// <param name="subject">The subject's identifier and a collection of attributes.</param>
    /// <param name="actions">The actions to consider and their contextual attributes.</param>
    /// <param name="defaultValue">Default flag variation.</param>
    /// <example>
    /// For Example:
    /// <code>
    /// var client = EppoClient.GetInstance();
    /// var subject = new ContextAttributes("subjectKey")
    /// {
    ///     ["age"] = 30,
    ///     ["country"] = "uk",
    ///     ["pricingTier"] = "1"  // NOTE: Deliberately setting to string causes this to be treated as a categorical attribute
    /// };
    ///
    /// var actions = new Dictionary<string, ContextAttributes>()
    /// {
    ///     ["nike"] = new ContextAttributes("nike")
    ///     {
    ///         ["brandLoyalty"] = 0.4,
    ///         ["from"] = "usa"
    ///     },
    ///     ["adidas"] = new ContextAttributes("adidas")
    ///     {
    ///         ["brandLoyalty"] = 2,
    ///         ["from"] = "germany"
    ///     },
    /// };
    /// var result = client.GetBanditAction(
    ///     "flagKey",
    ///     subject,
    ///     actions,
    ///     "defaultValue"
    /// );
    /// </code></example>
    /// </summary>
    public BanditResult GetBanditAction(string flagKey,
                                        ContextAttributes subject,
                                        IDictionary<string, ContextAttributes> actions,
                                        string defaultValue)
    {
        try
        {
            return GetBanditDetail(flagKey, subject, actions, defaultValue);
        }
        catch (Exception e)
        {
            s_logger.Error("[Eppo SDK] error getting Bandit action: " + e.Message);
            return new BanditResult(defaultValue);
        }

    }

    /// <summary>Gets the selected action, if applicable, for the given <paramref name="flagKey"/> and contexts.
    /// <param name="flagKey">The flag or bandit key to lookup.</param>
    /// <param name="subjectKey">The subject's identifier.</param>
    /// <param name="subjectAttributes">The subject's attributes for consideration
    /// <para>Note: Attributes are sorted based on type into Categorical (String, boolean) and 
    /// Numerical attributes (numbers). All other attributes are discarded and a warning is logged.</para></param>
    /// <param name="actions">The actions to consider and their contextual attributes.
    /// <para>Note: Attributes are sorted based on type into Categorical (String, boolean) and 
    /// Numerical attributes (numbers). All other attributes are discarded and a warning is logged.</para></param>
    /// <param name="defaultValue">Default flag variation.</param>
    /// <example>
    /// For Example:
    /// <code>
    /// var client = EppoClient.GetInstance();
    /// var subjectAttributes = new Dictionary<string, object?>()
    /// {
    ///     ["age"] = 30,
    ///     ["country"] = "uk",
    ///     ["pricingTier"] = "1"  // NOTE: Deliberately setting to string causes this to be treated as a categorical attribute
    /// };
    /// var actions = new Dictionary<string, IDictionary<string, object?>>()
    /// {
    ///     ["nike"] = new Dictionary<string, object?>()
    ///     {
    ///         ["brandLoyalty"] = 0.4,
    ///         ["from"] = "usa"
    ///     },
    ///     ["adidas"] = new Dictionary<string, object?>()
    ///     {
    ///         ["brandLoyalty"] = 2,
    ///         ["from"] = "germany"
    ///     }
    /// };
    /// var result = client.GetBanditAction(
    ///     "flagKey",
    ///     "subjecKey",
    ///     subjectAttributes,
    ///     actions,
    ///     "defaultValue");
    /// </code></example>
    /// </summary>
    public BanditResult GetBanditAction(string flagKey,
                                        string subjectKey,
                                        IDictionary<string, object?> subjectAttributes,
                                        IDictionary<string, IDictionary<string, object?>> actions,
                                        string defaultValue)
    {
        return GetBanditDetail(
            flagKey,
            ContextAttributes.FromDict(subjectKey, subjectAttributes),
            actions.ToDictionary(kvp => kvp.Key, kvp => ContextAttributes.FromDict(kvp.Key, kvp.Value)),
            defaultValue);
    }

    private BanditResult GetBanditDetail(string flagKey,
                                        ContextAttributes subject,
                                        IDictionary<string, ContextAttributes> actions,
                                        string defaultValue)
    {
        InputValidator.ValidateNotBlank(flagKey, "Invalid argument: flagKey cannot be blank");

        BanditResult? result = null;

        // Get the user's flag assignment for the given key.
        var variation = GetStringAssignment(
            flagKey,
            subject.Key,
            subject,
            defaultValue);

        // Only proceed to computing a bandit if there are actions provided and the variation maps to a bandit key
        if (actions.Count > 0
            && _config.GetBanditReferences().TryGetBanditKey(flagKey, variation, out string? banditKey)
            && banditKey != null)
        {
            result = EvaluateAndLogBandit(banditKey!, flagKey, subject, actions, variation);
        }

        return result ?? new(variation);

    }

    private BanditResult? EvaluateAndLogBandit(string banditKey,
                                               string flagKey,
                                               ContextAttributes subject,
                                               IDictionary<string, ContextAttributes> actions,
                                               string variation)
    {
        try
        {
            if (_config.TryGetBandit(banditKey, out Bandit? bandit) && bandit != null)
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
                   _appDetails.AsDict());

                try
                {
                    _eppoClientConfig.AssignmentLogger.LogBanditAction(banditActionLog);
                }
                catch (Exception) { }

                return new BanditResult(variation, result.ActionKey);
            }
            else
            {
                // There should be a bandit matching `banditKey`, but there is not, and that's a problem.
                s_logger.Error($"[Eppo SDK] Bandit model {banditKey} not found for {flagKey} {variation}");
            }
        }
        catch (BanditEvaluationException bee)
        {
            s_logger.Error("[Eppo SDK] Error evaluating bandit, returning variation only: " + bee.Message);
        }
        return null;
    }

    public void RefreshConfiguration()
    {
        _config.LoadConfiguration();
    }

    public static EppoClient GetInstance()
    {
        if (s_client == null)
        {
            throw new EppoClientIsNotInitializedException("Eppo client is not initialized");
        }

        return s_client;
    }

    public void Dispose()
    {
        _fetchExperimentsTask?.Dispose();
    }
}
