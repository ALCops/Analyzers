using System.Reflection;
using NavCodeAnalysis = Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Reflection;

/// <summary>
/// Centralized version compatibility provider with reflection and caching.
/// Provides safe access to VersionCompatibility static fields that may not exist in all versions.
/// 
/// WHY WE USE REFLECTION:
/// - The VersionCompatibility class gains new static fields with each Business Central release
/// - Direct field references would break compilation when using older dependency versions
/// - Using reflection maintains backward compatibility across dependency versions
/// - The SDK can support a runtime version before it ships the matching *OrGreater field: early builds of
///   SDK 13.0, 14.0 and 16.0 lack Spring2024OrGreater, Fall2024OrGreater and Fall2025OrGreater, and SDK 18.0
///   has RuntimeVersion.Fall2026 but no Fall2026OrGreater. A missing field therefore resolves to
///   VersionCompatibility.Parse of its runtime version, so a rule is never disabled on an SDK that supports
///   its runtime version.
/// 
/// USAGE:
/// Use VersionProvider.VersionCompatibility.* instead of Microsoft.Dynamics.Nav.CodeAnalysis.VersionCompatibility.*
/// Example: VersionProvider.VersionCompatibility.Spring2030OrGreater
/// </summary>
public static class VersionProvider
{
    /// <summary>
    /// Internal method for getting VersionCompatibility values with caching.
    /// DO NOT call this directly - use the nested VersionCompatibility class instead.
    /// 
    /// This method uses reflection to access static fields from strings, providing
    /// backward compatibility when new version fields are added in newer dependency versions.
    /// A missing field resolves to <paramref name="runtimeVersion"/> parsed by VersionCompatibility.Parse
    /// ("18.0" = 18.0 or greater, the same range the SDK's own *OrGreater fields use).
    /// </summary>
    private static NavCodeAnalysis.VersionCompatibility GetVersionCompatibility(string fieldName, string runtimeVersion)
    {
        var lazy = new Lazy<NavCodeAnalysis.VersionCompatibility>(() =>
        {
            var field = typeof(NavCodeAnalysis.VersionCompatibility)
                .GetField(fieldName, BindingFlags.Public | BindingFlags.Static);

            if (field?.GetValue(null) is NavCodeAnalysis.VersionCompatibility value)
                return value;

            return NavCodeAnalysis.VersionCompatibility.Parse(runtimeVersion);
        }, LazyThreadSafetyMode.PublicationOnly);

        return lazy.Value;
    }

    /// <summary>
    /// Provides reflection-based access to VersionCompatibility static fields.
    /// Mirrors the Microsoft.Dynamics.Nav.CodeAnalysis.VersionCompatibility API.
    /// When a field doesn't exist in the loaded SDK, returns the range of its runtime version instead.
    /// </summary>
    public static class VersionCompatibility
    {
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _fall2019OrGreater =
            new(() => GetVersionCompatibility("Fall2019OrGreater", "4.0"));
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _spring2021OrGreater =
            new(() => GetVersionCompatibility("Spring2021OrGreater", "7.0"));
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _fall2022OrGreater =
            new(() => GetVersionCompatibility("Fall2022OrGreater", "10.0"));
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _spring2023OrGreater =
            new(() => GetVersionCompatibility("Spring2023OrGreater", "11.0"));
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _fall2023OrGreater =
            new(() => GetVersionCompatibility("Fall2023OrGreater", "12.0"));
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _spring2024OrGreater =
            new(() => GetVersionCompatibility("Spring2024OrGreater", "13.0"));
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _fall2024OrGreater =
            new(() => GetVersionCompatibility("Fall2024OrGreater", "14.0"));
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _fall2025OrGreater =
            new(() => GetVersionCompatibility("Fall2025OrGreater", "16.0"));
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _fall2026OrGreater =
            new(() => GetVersionCompatibility("Fall2026OrGreater", "18.0"));

        public static NavCodeAnalysis.VersionCompatibility Fall2019OrGreater => _fall2019OrGreater.Value;
        public static NavCodeAnalysis.VersionCompatibility Spring2021OrGreater => _spring2021OrGreater.Value;
        public static NavCodeAnalysis.VersionCompatibility Fall2022OrGreater => _fall2022OrGreater.Value;
        public static NavCodeAnalysis.VersionCompatibility Spring2023OrGreater => _spring2023OrGreater.Value;
        public static NavCodeAnalysis.VersionCompatibility Fall2023OrGreater => _fall2023OrGreater.Value;
        public static NavCodeAnalysis.VersionCompatibility Spring2024OrGreater => _spring2024OrGreater.Value;
        public static NavCodeAnalysis.VersionCompatibility Fall2024OrGreater => _fall2024OrGreater.Value;
        public static NavCodeAnalysis.VersionCompatibility Fall2025OrGreater => _fall2025OrGreater.Value;
        public static NavCodeAnalysis.VersionCompatibility Fall2026OrGreater => _fall2026OrGreater.Value;
    }
}