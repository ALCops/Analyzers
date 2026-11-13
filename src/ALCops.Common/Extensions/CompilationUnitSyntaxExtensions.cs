using ALCops.Common.Helpers;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Extensions;

public static class CompilationUnitSyntaxExtensions
{
    /// <summary>
    /// The namespace declared by the file, or null when it has no <c>namespace</c> declaration.
    /// </summary>
    public static string? GetFileNamespace(this CompilationUnitSyntax compilationUnit)
        => compilationUnit.NamespaceDeclaration?.Name?.ToString();

    /// <summary>
    /// The namespaces imported by the file's <c>using</c> directives.
    /// </summary>
    public static IEnumerable<string> GetImportedNamespaces(this CompilationUnitSyntax compilationUnit)
    {
        var usings = compilationUnit.Usings;
        for (int i = 0; i < usings.Count; i++)
        {
            var name = usings[i].Name?.ToString();
            if (!string.IsNullOrEmpty(name))
                yield return name;
        }
    }

    /// <summary>
    /// True when a <c>using</c> directive imports <paramref name="namespaceName"/> (AL name comparison).
    /// </summary>
    public static bool HasUsingDirective(this CompilationUnitSyntax compilationUnit, string namespaceName)
    {
        var usings = compilationUnit.Usings;
        for (int i = 0; i < usings.Count; i++)
        {
            if (usings[i].Name?.ToString().IsSameName(namespaceName) == true)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Adds <c>using <paramref name="namespaceName"/>;</c> when the file declares a namespace other than
    /// <paramref name="namespaceName"/> and does not import it yet. Files without a namespace declaration
    /// resolve every object globally, so they are returned unchanged.
    /// Placement mirrors the SDK's own "add using" code action: the first using goes on its own line after the
    /// namespace declaration; otherwise the new directive is inserted before the first using that sorts after it,
    /// or appended after the last using when any using carries a preprocessor directive, because reordering
    /// around <c>#if</c> blocks could change what compiles. Sorting uses the AL name comparer
    /// (case-insensitive) instead of the SDK's ordinal comparison, matching CodeCop's using-order rule.
    /// </summary>
    public static CompilationUnitSyntax AddUsingDirectiveIfNeeded(this CompilationUnitSyntax compilationUnit, string namespaceName)
    {
        if (compilationUnit.NamespaceDeclaration is null)
            return compilationUnit;

        if (compilationUnit.GetFileNamespace().IsSameName(namespaceName))
            return compilationUnit;

        if (compilationUnit.HasUsingDirective(namespaceName))
            return compilationUnit;

        var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactoryHelper.NamespaceName(namespaceName))
            .WithSemicolonToken(SyntaxFactory.Token(EnumProvider.SyntaxKind.SemicolonToken));

        var usings = compilationUnit.Usings;
        var endOfLine = SyntaxFactory.EndOfLine(Environment.NewLine, elastic: false);

        if (usings.Count == 0)
            return compilationUnit.AddUsings(usingDirective.WithLeadingTrivia(endOfLine).WithTrailingTrivia(endOfLine));

        for (int i = 0; i < usings.Count; i++)
        {
            if (usings[i].ContainsDirectives)
            {
                var lastUsing = usings[usings.Count - 1];
                return compilationUnit.WithUsings(usings.Add(usingDirective.WithTrailingTrivia(lastUsing.GetTrailingTrivia())));
            }
        }

        var newUsing = usingDirective.WithTrailingTrivia(endOfLine);
        for (int i = 0; i < usings.Count; i++)
        {
            if (SemanticFacts.NameComparer.Compare(usings[i].Name?.ToString() ?? string.Empty, namespaceName) <= 0)
                continue;

            if (i == 0)
            {
                // Keep the blank line that separated the namespace declaration from the first using above the new first using.
                var leadingTrivia = usings[0].GetLeadingTrivia();
                if (leadingTrivia.Count > 0 && leadingTrivia[0].Kind == EnumProvider.SyntaxKind.EndOfLineTrivia)
                {
                    newUsing = newUsing.WithLeadingTrivia(endOfLine);
                    usings = usings.Replace(usings[0], usings[0].WithLeadingTrivia(leadingTrivia.Skip(1)));
                }
            }

            return compilationUnit.WithUsings(usings.Insert(i, newUsing));
        }

        return compilationUnit.WithUsings(usings.Add(newUsing));
    }
}
