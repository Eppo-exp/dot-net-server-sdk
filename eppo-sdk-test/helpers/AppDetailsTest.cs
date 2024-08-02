using System.Text.RegularExpressions;
using eppo_sdk.helpers;
using NUnit.Framework.Internal;
using static NUnit.Framework.Assert;

namespace eppo_sdk_test.helpers;

public partial class AppDetailsTest
{
    [GeneratedRegex("^\\d+\\.\\d+\\.\\d+$")]
    private static partial Regex SemVerRegex();

    [Test]
    public void ShouldMaintainSingleton()
    {
        var appDetails = AppDetails.GetInstance();
        var name = appDetails.GetName();

        Multiple(() =>
        {
            That(name, Is.Not.Null);
            That(name, Is.EqualTo("dotnet-server"));
        });
    }

    [Test]
    public void ShouldReturnASemVer()
    {
        var appDetails = AppDetails.GetInstance();
        var version = appDetails.GetVersion();
        Multiple(() =>
        {
            That(version, Is.Not.Null);
            That(SemVerRegex().IsMatch(version), Is.True); // Basic check for 3-segment format
        });
    }

    [Test]
    public void ShouldReturnRightName()
    {
        var appDetails = AppDetails.GetInstance();
        var name = appDetails.GetName();

        Multiple(() =>
        {
            That(name, Is.Not.Null);
            That(name, Is.EqualTo("dotnet-server"));
        });
    }
}
