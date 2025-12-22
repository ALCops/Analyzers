using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.FormattingCop.Analyzers;

[DiagnosticAnalyzer]
public class CasingMismatchKeyword : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.CasingMismatch);

    private static readonly HashSet<SyntaxKind> DataTypeSyntaxKinds =
            Enum.GetNames(typeof(SyntaxKind))
                .Where(name => name.AsSpan().EndsWith("DataType"))
                .Select(Enum.Parse<SyntaxKind>)
                .ToHashSet();

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSymbolAction(
            this.AnalyzeTokens,
                SymbolKind.Codeunit,
                SymbolKind.Entitlement,
                SymbolKind.Enum,
                SymbolKind.EnumExtension,
                SymbolKind.Interface,
                SymbolKind.Page,
                SymbolKind.PageExtension,
                SymbolKind.PermissionSet,
                SymbolKind.PermissionSetExtension,
                SymbolKind.Profile,
                SymbolKind.ProfileExtension,
                SymbolKind.Query,
                SymbolKind.Report,
                SymbolKind.ReportExtension,
                SymbolKind.Table,
                SymbolKind.TableExtension,
                SymbolKind.XmlPort);
    }

    private void AnalyzeTokens(SymbolAnalysisContext ctx)
    {
        var node = ctx.Symbol.DeclaringSyntaxReference?.GetSyntax(ctx.CancellationToken);
        if (node == null)
            return;

        foreach (var token in node.DescendantTokens())
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            var tokenText = token.ValueText;
            if (string.IsNullOrEmpty(tokenText))
                continue;

            if (token.Parent is null || !token.Kind.IsKeyword() || DataTypeSyntaxKinds.Contains(token.Parent.Kind))
                continue;

            var canonicalToken = SyntaxFactory.Token(token.Kind);
            if (canonicalToken.Kind == SyntaxKind.None)
                continue;

            if (canonicalToken.ValueText.AsSpan().Equals(tokenText.AsSpan(), StringComparison.Ordinal))
                continue;

            var properties = ImmutableDictionary<string, string>.Empty
                .Add("CanonicalText", canonicalToken.ToString());

            ctx.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.CasingMismatch,
                    token.GetLocation(),
                    properties,
                    canonicalToken,
                    tokenText));
        }
    }
}