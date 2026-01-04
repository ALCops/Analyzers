#if !NET8_0_OR_GREATER
using System.Reflection;
#endif
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Packaging;

namespace ALCops.Common.Extensions;

public static class ManifestHelper
{
#if NET8_0_OR_GREATER
    public static NavAppManifest? GetManifest(Compilation compilation) =>
        Microsoft.Dynamics.Nav.Analyzers.Common.ManifestHelper.GetManifest(compilation);
#else
    private static readonly Lazy<Func<Compilation, NavAppManifest?>?> _getManifest =
        new(BuildGetManifest, isThreadSafe: true);

    public static NavAppManifest? GetManifest(Compilation compilation)
    {
        var getManifestFunc = _getManifest.Value;
        return getManifestFunc is null ? null : getManifestFunc(compilation);
    }

    private static Func<Compilation, NavAppManifest?>? BuildGetManifest()
    {
        // Prior to AL version 13.0 the AppSourceCopConfigurationProvider was used to provide the manifest instead of the ManifestHelper
        var getManifestDelegate =
            TryCreateDelegate(
                "Microsoft.Dynamics.Nav.Analyzers.Common.AppSourceCopConfiguration.AppSourceCopConfigurationProvider, Microsoft.Dynamics.Nav.Analyzers.Common",
                "GetManifest")
            ?? TryCreateDelegate(
                "Microsoft.Dynamics.Nav.Analyzers.Common.ManifestHelper, Microsoft.Dynamics.Nav.Analyzers.Common",
                "GetManifest");

        return getManifestDelegate;
    }

    private static Func<Compilation, NavAppManifest?>? TryCreateDelegate(string qualifiedTypeName, string methodName)
    {
        var type = Type.GetType(qualifiedTypeName, throwOnError: false);
        if (type is null)
            return null;

        var methodInfo = type.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(Compilation) },
            modifiers: null);

        if (methodInfo is null)
            return null;

        return (Func<Compilation, NavAppManifest?>)methodInfo.CreateDelegate(typeof(Func<Compilation, NavAppManifest?>));
    }
#endif
}