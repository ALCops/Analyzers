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
        => compilationUnit.GetImportedNamespaces().Any(name => name.IsSameName(namespaceName));

    /// <summary>
    /// Adds <c>using <paramref name="namespaceName"/>;</c> when the file declares a namespace other than
    /// <paramref name="namespaceName"/> and does not import it yet. Files without a namespace declaration
    /// resolve every object globally, so they are returned unchanged.
    /// Placement mirrors the SDK's own "add using" code action: the first using goes on its own line after the
    /// namespace declaration; otherwise the new directive is inserted before the first using that sorts after it.
    /// When any using carries a preprocessor directive, sorting is skipped because inserting between <c>#if</c> and
    /// <c>#endif</c> would make the new using conditional: it is appended after the last using, or placed before
    /// the first using when a directive directly follows the last one. Sorting uses the AL name comparer
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

        if (usings.Any(u => u.ContainsDirectives))
        {
            // A directive on the line after the last using (typically the #endif closing a block of usings) is
            // leading trivia of the next token, not of any using, so appending there would put the new using
            // inside the conditional block. The first using is always outside any block opened among the usings.
            var lastUsing = usings[usings.Count - 1];
            if (lastUsing.GetLastToken().GetNextToken(includeZeroWidth: true).LeadingTrivia.Any(trivia => trivia.IsDirective))
                return compilationUnit.WithUsings(InsertAsFirstUsing(usings, usingDirective, endOfLine));

            return compilationUnit.WithUsings(usings.Add(usingDirective.WithTrailingTrivia(lastUsing.GetTrailingTrivia())));
        }

        for (int i = 0; i < usings.Count; i++)
        {
            if (SemanticFacts.NameComparer.Compare(usings[i].Name?.ToString() ?? string.Empty, namespaceName) <= 0)
                continue;

            if (i == 0)
                return compilationUnit.WithUsings(InsertAsFirstUsing(usings, usingDirective, endOfLine));

            return compilationUnit.WithUsings(usings.Insert(i, usingDirective.WithTrailingTrivia(endOfLine)));
        }

        return compilationUnit.WithUsings(usings.Add(usingDirective.WithTrailingTrivia(endOfLine)));
    }

    private static SyntaxList<UsingDirectiveSyntax> InsertAsFirstUsing(SyntaxList<UsingDirectiveSyntax> usings, UsingDirectiveSyntax usingDirective, SyntaxTrivia endOfLine)
    {
        var newUsing = usingDirective.WithTrailingTrivia(endOfLine);

        // Keep the blank line that separated the namespace declaration from the first using above the new first using.
        var leadingTrivia = usings[0].GetLeadingTrivia();
        if (leadingTrivia.Count > 0 && leadingTrivia[0].Kind == EnumProvider.SyntaxKind.EndOfLineTrivia)
        {
            newUsing = newUsing.WithLeadingTrivia(endOfLine);
            usings = usings.Replace(usings[0], usings[0].WithLeadingTrivia(leadingTrivia.Skip(1)));
        }

        return usings.Insert(0, newUsing);
    }
}