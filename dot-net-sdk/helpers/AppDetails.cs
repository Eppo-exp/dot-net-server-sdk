using System.Reflection;

namespace eppo_sdk.helpers;

public class AppDetails
{
    private const string SDK_LANG = "c#";
    static AppDetails? _instance;

    private readonly string? _version;
    private readonly string? _name;
    private readonly string _uuid;

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
        this._version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        this._name = Assembly.GetExecutingAssembly().GetName().Name;
        this._uuid = System.Guid.NewGuid().ToString();
    }

    public string GetName()
    {
        return this._name!;
    }

    public string GetVersion()
    {
        return this._version!;
    }

    public IReadOnlyDictionary<string, string> AsDict()
    {
        return new Dictionary<string, string>() {
            ["sdkLanguage"] = SDK_LANG,
            ["sdkName"] = GetName(),
            ["sdkVersion"] = GetVersion(),
            ["clientUID"] = _uuid
        };
    }
}
