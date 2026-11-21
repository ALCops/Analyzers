using ALCops.Common.Reflection;
using NavCodeAnalysis = Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Test;

public class VersionProviderTests
{
    // Each field must resolve to "its runtime version or greater", whether the loaded SDK ships the field
    // or VersionProvider falls back to parsing the runtime version.
    private static IEnumerable<TestCaseData> Fields()
    {
        yield return Case(nameof(VersionProvider.VersionCompatibility.Fall2019OrGreater), () => VersionProvider.VersionCompatibility.Fall2019OrGreater, "4.0", "3.9");
        yield return Case(nameof(VersionProvider.VersionCompatibility.Spring2021OrGreater), () => VersionProvider.VersionCompatibility.Spring2021OrGreater, "7.0", "6.9");
        yield return Case(nameof(VersionProvider.VersionCompatibility.Fall2022OrGreater), () => VersionProvider.VersionCompatibility.Fall2022OrGreater, "10.0", "9.9");
        yield return Case(nameof(VersionProvider.VersionCompatibility.Spring2023OrGreater), () => VersionProvider.VersionCompatibility.Spring2023OrGreater, "11.0", "10.9");
        yield return Case(nameof(VersionProvider.VersionCompatibility.Fall2023OrGreater), () => VersionProvider.VersionCompatibility.Fall2023OrGreater, "12.0", "11.9");
        yield return Case(nameof(VersionProvider.VersionCompatibility.Spring2024OrGreater), () => VersionProvider.VersionCompatibility.Spring2024OrGreater, "13.0", "12.9");
        yield return Case(nameof(VersionProvider.VersionCompatibility.Fall2024OrGreater), () => VersionProvider.VersionCompatibility.Fall2024OrGreater, "14.0", "13.9");
        yield return Case(nameof(VersionProvider.VersionCompatibility.Fall2025OrGreater), () => VersionProvider.VersionCompatibility.Fall2025OrGreater, "16.0", "15.9");
        yield return Case(nameof(VersionProvider.VersionCompatibility.Fall2026OrGreater), () => VersionProvider.VersionCompatibility.Fall2026OrGreater, "18.0", "17.9");
    }

    private static TestCaseData Case(string name, Func<NavCodeAnalysis.VersionCompatibility> field, string first, string previous) =>
        new TestCaseData(field, first, previous).SetArgDisplayNames(name);

    [TestCaseSource(nameof(Fields))]
    public void SupportsItsRuntimeVersionAndLater(Func<NavCodeAnalysis.VersionCompatibility> field, string first, string previous)
    {
        var compatibility = field();
        var from = Version.Parse(first);

        Assert.Multiple(() =>
        {
            Assert.That(compatibility.IsSupported(from), Is.True, $"{first} should be supported");
            Assert.That(compatibility.IsSupported(new Version(from.Major, from.Minor + 1)), Is.True, "the next minor version should be supported");
            Assert.That(compatibility.IsSupported(Version.Parse(previous)), Is.False, $"{previous} should be rejected");
        });
    }
}
