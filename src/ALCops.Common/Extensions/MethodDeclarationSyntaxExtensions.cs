using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Extensions;

public static class MethodDeclarationSyntaxExtensions
{
    private const string TryFunctionAttributeName = "TryFunction";

    /// <summary>
    /// Syntax-based TryFunction detection.
    /// The SDK exposes <c>IMethodSymbol.IsTryFunction</c> on <c>net8.0</c> and <c>net10.0</c>,
    /// but that property is <em>not</em> present on the <c>netstandard2.1</c> reference DLL
    /// pinned in this repository, so analyzers targeting all three CI TFMs must use this
    /// syntax-based check to remain portable.
    /// </summary>
    public static bool IsTryFunction(this MethodDeclarationSyntax method)
    {
        foreach (var attribute in method.Attributes)
        {
            var attributeName = attribute.GetIdentifierOrLiteralValue();

            if (attributeName is not null && SemanticFacts.IsSameName(attributeName, TryFunctionAttributeName))
            {
                return true;
            }
        }

        return false;
    }
}
