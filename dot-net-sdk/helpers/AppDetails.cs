using System.Reflection;

namespace eppo_sdk.helpers;

public class AppDetails
{
    static AppDetails? _instance;

    private readonly string? _version;
    private readonly string? _name;

    public static AppDetails GetInstance()
    {
        if (_instance != null) return AppDetails._instance;

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
    }

    public string GetName()
    {
        return this._name!;
    }

    public string GetVersion()
    {
        return this._version!;
    }
}