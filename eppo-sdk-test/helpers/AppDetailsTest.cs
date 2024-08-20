using System.Text.RegularExpressions;
using eppo_sdk.client;
using eppo_sdk.helpers;
using NUnit.Framework.Internal;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test.helpers;

public partial class AppDetailsTest
{
    [GeneratedRegex("^\\d+\\.\\d+\\.\\d+$")]
    private static partial Regex SemVerRegex();

    [Test]
    public void ShouldReturnASemVer()
    {
        var appDetails = new AppDetails();
        var version = appDetails.Version;
        Multiple(() =>
        {
            That(version, Is.Not.Null);
            That(SemVerRegex().IsMatch(version), Is.True); // Basic check for 3-segment format
        });
    }

    [Test]
    public void ShouldReturnRightNameForServer()
    {
        var appDetails = new AppDetails();
        var name = appDetails.Name;

        Multiple(() =>
        {
            That(name, Is.Not.Null);
            That(name, Is.EqualTo("dotnet-server"));
        });
    }

    [Test]
    public void ShouldReturnRightNameForClient()
    {
        var appDetails = new AppDetails(DeploymentEnvironment.Client());
        var name = appDetails.Name;

        Multiple(() =>
        {
            That(name, Is.Not.Null);
            That(name, Is.EqualTo("dotnet-client"));
        });
    }
}
