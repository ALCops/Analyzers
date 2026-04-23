using System.Reflection;

namespace ALCops.Common.Helpers;

/// <summary>
/// Provides access to the SDK's OData/EDMX name transformation via reflection.
/// The method <c>MangleIntoValidXmlIdentifier</c> lives in
/// <c>Microsoft.Dynamics.Nav.AL.Common.NameTransformations</c>, which is loaded by
/// the AL compiler process (referenced by <c>Microsoft.Dynamics.Nav.CodeAnalysis.dll</c>).
/// </summary>
public static class ODataNameHelper
{
    private static readonly Lazy<Func<string, string>?> _mangleIntoValidXmlIdentifier =
        new(BuildDelegate, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// Transforms an AL identifier name into its OData/EDMX entity name representation
    /// using the SDK's <c>NameTransformations.MangleIntoValidXmlIdentifier</c> method.
    /// Returns <c>null</c> if the SDK method is not available (e.g., older SDK versions).
    /// </summary>
    public static string? MangleIntoValidXmlIdentifier(string name)
    {
        var func = _mangleIntoValidXmlIdentifier.Value;
        if (func is null)
            return null;

        return func(name);
    }

    /// <summary>
    /// Indicates whether the SDK's OData name transformation is available.
    /// </summary>
    public static bool IsAvailable => _mangleIntoValidXmlIdentifier.Value is not null;

    private static Func<string, string>? BuildDelegate()
    {
        var type = Type.GetType(
            "Microsoft.Dynamics.Nav.AL.Common.NameTransformations, Microsoft.Dynamics.Nav.AL.Common",
            throwOnError: false);

        if (type is null)
            return null;

        var methodInfo = type.GetMethod(
            "MangleIntoValidXmlIdentifier",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);

        if (methodInfo is null)
            return null;

        return (Func<string, string>)methodInfo.CreateDelegate(typeof(Func<string, string>));
    }
}
