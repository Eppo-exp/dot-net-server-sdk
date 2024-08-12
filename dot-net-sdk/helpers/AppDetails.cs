using System.Reflection;
using eppo_sdk.client;
using eppo_sdk.exception;

namespace eppo_sdk.helpers;

public class AppDetails
{
    private static AppDetails? s_instance;
    
    public SDKDeploymentMode Deployment {get ; init;}

    private readonly string _version;
    private const string _serverSdkName = "dotnet-server";
    private const string _clientSdkName = "dotnet-client";

    public static AppDetails GetInstance()
    {
        if (s_instance == null) 
        {
            throw new EppoClientIsNotInitializedException("AppDetails is not initialized");
        }
        return s_instance;
    }

    private AppDetails(SDKDeploymentMode deployment = SDKDeploymentMode.SERVER)
    {
        // .net returns a 4-segmented version string (MAJOR.MINOR.BUILD.REVISION) here but we want to stick to semver standards (3-segment).
        // We use a convention of Major.Minor.Patch when setting the package version; dotnet parses this to Major.Minor.Build and apprends
        // the `.0` for Revision automatically. We can safely ignore it.
        var fullVersion = Assembly.GetExecutingAssembly().GetName().Version!;
        _version = $"{fullVersion.Major}.{fullVersion.Minor}.{fullVersion.Build}";

        Deployment = deployment;
    }

    public string Name => Deployment.GetSDKName();

    public string Version => _version;

    public IReadOnlyDictionary<string, string> AsDict()
    {
        return new Dictionary<string, string>()
        {
            ["sdkName"] = Name,
            ["sdkVersion"] = Version
        };
    }

    /// <summary>
    /// Initializes the AppDetails singleton for Client Mode.
    /// </summary>
    public static AppDetails InitClient()
    {
        s_instance = new AppDetails(SDKDeploymentMode.CLIENT);
        return s_instance;
    }

    /// <summary>
    /// Initializes the AppDetails singleton for typical use (Server Mode). 
    /// </summary>
    public static AppDetails Init()
    {
        s_instance = new AppDetails(SDKDeploymentMode.SERVER);
        return s_instance;
    }
}
