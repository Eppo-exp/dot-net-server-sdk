using System.Reflection;

namespace eppo_sdk.helpers;

public class AppDetails
{
    static AppDetails? instance;

    private readonly string? version;
    private readonly string? name;

    public static AppDetails GetInstance()
    {
        if (instance != null) return instance;

        instance = new AppDetails();
        if (instance.name == null || instance.version == null)
        {
            throw new SystemException("Unable to find the version and app name details");
        }

        return instance;
    }

    private AppDetails()
    {
        // .net returns a 4-segmented version string (MAJOR.MINOR.BUILD.REVISION) here but we want to stick to semver standards (3-segment).
        // We use a convention of Major.Minor.Patch when setting the package version; dotnet parses this to Major.Minor.Build and apprends
        // the `.0` for Revision automatically. We can safely ignore it.
        var fullVersion = Assembly.GetExecutingAssembly().GetName().Version!;
        version = $"{fullVersion.Major}.{fullVersion.Minor}.{fullVersion.Build}";
        
        // Hardcoded for now; update soon with client/server split.
        name = "dotnet-server";
    }

    public string GetName()
    {
        return this.name!;
    }

    public string GetVersion()
    {
        return this.version!;
    }

    public IReadOnlyDictionary<string, string> AsDict()
    {
        return new Dictionary<string, string>() {
            ["sdkName"] = GetName(),
            ["sdkVersion"] = GetVersion()
        };
    }
}
