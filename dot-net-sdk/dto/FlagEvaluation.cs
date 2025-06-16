namespace eppo_sdk.dto;

public record FlagEvaluation
{
    public Variation Variation;
    public bool DoLog;
    public string AllocationKey;
    public IReadOnlyDictionary<string, string> ExtraLogging;

    public FlagEvaluation(
        Variation variation,
        bool doLog,
        string allocationKey,
        IReadOnlyDictionary<string, object>? extraLogging
    )
    {
        Variation = variation;
        DoLog = doLog;
        AllocationKey = allocationKey;
        ExtraLogging =
            extraLogging == null
                ? new Dictionary<string, string>()
                : (IReadOnlyDictionary<string, string>)
                    extraLogging.ToDictionary(
                        pair => pair.Key,
                        pair => Convert.ToString(pair.Value)
                    );
    }
}
