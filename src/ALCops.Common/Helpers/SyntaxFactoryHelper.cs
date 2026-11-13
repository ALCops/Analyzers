using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Helpers;

/// <summary>
/// Syntax construction helpers that the SDK's <see cref="SyntaxFactory"/> lacks at the oldest supported version.
/// </summary>
public static class SyntaxFactoryHelper
{
    /// <summary>
    /// Builds a <see cref="NameSyntax"/> from a dotted namespace name
    /// (e.g. "MyPTE.Sales" becomes <c>QualifiedName(IdentifierName("MyPTE"), IdentifierName("Sales"))</c>).
    /// <c>SyntaxFactory.ParseQualifiedName</c> does the same but is missing from the netstandard2.1 SDK.
    /// </summary>
    public static NameSyntax NamespaceName(string qualifiedName)
    {
        var segments = qualifiedName.Split('.');
        NameSyntax result = SyntaxFactory.IdentifierName(segments[0]);
        for (int i = 1; i < segments.Length; i++)
            result = SyntaxFactory.QualifiedName(result, SyntaxFactory.IdentifierName(segments[i]));
        return result;
    }
}
