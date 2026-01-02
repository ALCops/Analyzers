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
/// - Returns a "never supported" VersionCompatibility when a field doesn't exist
/// 
/// USAGE:
/// Use VersionProvider.VersionCompatibility.* instead of Microsoft.Dynamics.Nav.CodeAnalysis.VersionCompatibility.*
/// Example: VersionProvider.VersionCompatibility.Spring2030OrGreater
/// </summary>
public static class VersionProvider
{
    /// <summary>
    /// A VersionCompatibility that never matches any version.
    /// Used as fallback when a requested version field doesn't exist.
    /// This effectively disables the analyzer for all BC versions.
    /// </summary>
    private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _neverSupported =
        new(() => new NavCodeAnalysis.VersionCompatibility([]),
            LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// Internal method for getting VersionCompatibility values with caching.
    /// DO NOT call this directly - use the nested VersionCompatibility class instead.
    /// 
    /// This method uses reflection to access static fields from strings, providing
    /// backward compatibility when new version fields are added in newer dependency versions.
    /// </summary>
    private static NavCodeAnalysis.VersionCompatibility GetVersionCompatibility(string fieldName)
    {
        var lazy = new Lazy<NavCodeAnalysis.VersionCompatibility>(() =>
        {
            var field = typeof(NavCodeAnalysis.VersionCompatibility)
                .GetField(fieldName, BindingFlags.Public | BindingFlags.Static);

            return field?.GetValue(null) as NavCodeAnalysis.VersionCompatibility ?? _neverSupported.Value;
        }, LazyThreadSafetyMode.PublicationOnly);

        return lazy.Value;
    }

    /// <summary>
    /// Provides reflection-based access to VersionCompatibility static fields.
    /// Mirrors the Microsoft.Dynamics.Nav.CodeAnalysis.VersionCompatibility API.
    /// When a field doesn't exist in the current version, returns a VersionCompatibility
    /// that never matches, effectively disabling the analyzer.
    /// </summary>
    public static class VersionCompatibility
    {
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _fall2019OrGreater =
            new(() => GetVersionCompatibility("Fall2019OrGreater"));
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _spring2021OrGreater =
            new(() => GetVersionCompatibility("Spring2021OrGreater"));
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _fall2022OrGreater =
            new(() => GetVersionCompatibility("Fall2022OrGreater"));
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _spring2023OrGreater =
            new(() => GetVersionCompatibility("Spring2023OrGreater"));
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _fall2023OrGreater =
            new(() => GetVersionCompatibility("Fall2023OrGreater"));
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _spring2024OrGreater =
            new(() => GetVersionCompatibility("Spring2024OrGreater"));
        private static readonly Lazy<NavCodeAnalysis.VersionCompatibility> _fall2024OrGreater =
            new(() => GetVersionCompatibility("Fall2024OrGreater"));

        public static NavCodeAnalysis.VersionCompatibility Fall2019OrGreater => _fall2019OrGreater.Value;
        public static NavCodeAnalysis.VersionCompatibility Spring2021OrGreater => _spring2021OrGreater.Value;
        public static NavCodeAnalysis.VersionCompatibility Fall2022OrGreater => _fall2022OrGreater.Value;
        public static NavCodeAnalysis.VersionCompatibility Spring2023OrGreater => _spring2023OrGreater.Value;
        public static NavCodeAnalysis.VersionCompatibility Fall2023OrGreater => _fall2023OrGreater.Value;
        public static NavCodeAnalysis.VersionCompatibility Spring2024OrGreater => _spring2024OrGreater.Value;
        public static NavCodeAnalysis.VersionCompatibility Fall2024OrGreater => _fall2024OrGreater.Value;
    }
}