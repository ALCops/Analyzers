using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Extensions;

public static class MethodDeclarationSyntaxExtensions
{
    private const string TryFunctionAttributeName = "TryFunction";

    public static bool IsTryFunction(this MethodDeclarationSyntax method)
    {
        foreach (var attribute in method.Attributes)
        {
            var attributeName = attribute.GetIdentifierOrLiteralValue();
            if (attributeName is not null && SemanticFacts.IsSameName(attributeName, TryFunctionAttributeName))
                return true;
        }

        return false;
    }
}
