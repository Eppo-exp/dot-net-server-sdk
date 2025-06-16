namespace eppo_sdk.exception;

public class ExperimentConfigurationNotFound : Exception
{
    public ExperimentConfigurationNotFound(string message)
        : base(message) { }
}
