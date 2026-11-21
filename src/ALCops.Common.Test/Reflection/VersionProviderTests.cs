using ALCops.Common.Reflection;

namespace ALCops.Common.Test;

public class VersionProviderTests
{
    [TestCase("18.0")]
    [TestCase("18.1")]
    public void Fall2026OrGreater_SupportsFall2026AndLater(string version)
    {
        Assert.That(
            VersionProvider.VersionCompatibility.Fall2026OrGreater.IsSupported(Version.Parse(version)),
            Is.True);
    }

    [TestCase("17.0")]
    [TestCase("17.9")]
    public void Fall2026OrGreater_RejectsEarlierVersions(string version)
    {
        Assert.That(
            VersionProvider.VersionCompatibility.Fall2026OrGreater.IsSupported(Version.Parse(version)),
            Is.False);
    }
}
