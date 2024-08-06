using System.Reflection;

namespace eppo_sdk.helpers;

public class AppDetails
{
    private static AppDetails? _instance;

    private readonly string? _version;
    private readonly string? _name;

    public static AppDetails GetInstance()
    {
        if (_instance != null) return _instance;

        _instance = new AppDetails();
        if (_instance._name == null || _instance._version == null)
        {
            throw new SystemException("Unable to find the version and app name details");
        }

        return _instance;
    }

    private AppDetails()
    {
        // .net returns a 4-segmented version string (MAJOR.MINOR.BUILD.REVISION) here but we want to stick to semver standards (3-segment).
        // We use a convention of Major.Minor.Patch when setting the package version; dotnet parses this to Major.Minor.Build and apprends
        // the `.0` for Revision automatically. We can safely ignore it.
        var fullVersion = Assembly.GetExecutingAssembly().GetName().Version!;
        _version = $"{fullVersion.Major}.{fullVersion.Minor}.{fullVersion.Build}";

        // Hardcoded for now; update soon with client/server split.
        _name = "dotnet-server";
    }

    public string Name => _name!;

    public string Version => _version!;

    public IReadOnlyDictionary<string, string> AsDict()
    {
        return new Dictionary<string, string>()
        {
            ["sdkName"] = Name,
            ["sdkVersion"] = Version
        };
    }
}
