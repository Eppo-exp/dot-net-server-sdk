namespace eppo_sdk.client;

public record DeploymentEnvironment(string SdkName, bool Polling)
{
    public static DeploymentEnvironment Server()
    {
        return new DeploymentEnvironment("dotnet-server", true);
    }
    public static DeploymentEnvironment Client()
    {
        return new DeploymentEnvironment("dotnet-client", false);
    }
}
