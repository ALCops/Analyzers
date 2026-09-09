using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.DocumentationCop.Helpers;

internal static class DocumentationTrivia
{
    internal static bool HasDocumentation(SyntaxNode declaration)
    {
        var trivia = declaration.GetLeadingTrivia();

        return trivia.Any(t =>
            t.Kind == EnumProvider.SyntaxKind.SingleLineDocumentationCommentTrivia ||
            t.Kind == EnumProvider.SyntaxKind.MultiLineDocumentationCommentTrivia);
    }
}
