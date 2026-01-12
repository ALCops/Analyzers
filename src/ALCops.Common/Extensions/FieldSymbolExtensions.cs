using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Extensions;

public static class FieldSymbolExtensions
{
#if NETSTANDARD2_1
    // .NET Standard 2.1 does not expose IFieldSymbol.Type.ToDisplayString().
    // To obtain a usable representation of the field type, we fall back to the syntax tree and extract the type text from FieldSyntax.
    // This is a best-effort approximation: it preserves structure but may lose semantic details such as canonical casing (e.g. CODE[20] vs Code[20]).
    public static string? TypeAsString(this IFieldSymbol fieldSymbol)
    {
        if (fieldSymbol.DeclaringSyntaxReference?.GetSyntax() is not FieldSyntax fieldSyntax)
            return null;

        return fieldSyntax.Type.ToString();
    }
#endif
}