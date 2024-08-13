using System.Reflection;
using eppo_sdk.client;

namespace eppo_sdk.helpers;

public class AppDetails
{
    public DeploymentEnvironment Deployment { get; init;}

    private readonly string _version;

    public AppDetails(DeploymentEnvironment? deployment = null)
    {
        // .net returns a 4-segmented version string (MAJOR.MINOR.BUILD.REVISION) here but we want to stick to semver standards (3-segment).
        // We use a convention of Major.Minor.Patch when setting the package version; dotnet parses this to Major.Minor.Build and apprends
        // the `.0` for Revision automatically. We can safely ignore it.
        var fullVersion = Assembly.GetExecutingAssembly().GetName().Version!;
        _version = $"{fullVersion.Major}.{fullVersion.Minor}.{fullVersion.Build}";

        this.Deployment = deployment ?? DeploymentEnvironment.Server();
    }

    public string Name => Deployment.SdkName;

    public string Version => _version;

    public IReadOnlyDictionary<string, string> AsDict()
    {
        return new Dictionary<string, string>()
        {
            ["sdkName"] = Name,
            ["sdkVersion"] = Version
        };
    }
}
