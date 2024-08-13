namespace eppo_sdk.client;

[AttributeUsage(AttributeTargets.Field)]
public class SDKNameAttribute : Attribute
{
    public string Value { get; set; }

    public SDKNameAttribute(string value)
    {
        Value = value;
    }
}

public static class SDKDeploymentExtension
{
    /// <summary>
    /// Gets the `SDKName` attribute from this enum value.
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public static string GetSDKName(this SDKDeploymentMode val)
    {
        var field = val.GetType().GetField(val.ToString());
        if (field != null)
        {
            var attrs = (SDKNameAttribute[])field.GetCustomAttributes(typeof(SDKNameAttribute), false);
            return attrs.Length > 0 ? attrs[0].Value : string.Empty;
        }

        return string.Empty;
    }
} 

/// <summary>
/// Types of applications this SDK can run in and associated attributes.
/// </summary>
public enum SDKDeploymentMode {
    [SDKName("dotnet-server")]
    SERVER,
    [SDKName("dotnet-client")]
    CLIENT
}
