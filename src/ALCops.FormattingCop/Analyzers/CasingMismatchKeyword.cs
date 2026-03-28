using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.FormattingCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class CasingMismatchKeyword : DiagnosticAnalyzer
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
                EnumProvider.SymbolKind.Codeunit,
                EnumProvider.SymbolKind.Entitlement,
                EnumProvider.SymbolKind.Enum,
                EnumProvider.SymbolKind.EnumExtension,
                EnumProvider.SymbolKind.Interface,
                EnumProvider.SymbolKind.Page,
                EnumProvider.SymbolKind.PageExtension,
                EnumProvider.SymbolKind.PermissionSet,
                EnumProvider.SymbolKind.PermissionSetExtension,
                EnumProvider.SymbolKind.Profile,
                EnumProvider.SymbolKind.ProfileExtension,
                EnumProvider.SymbolKind.Query,
                EnumProvider.SymbolKind.Report,
                EnumProvider.SymbolKind.ReportExtension,
                EnumProvider.SymbolKind.Table,
                EnumProvider.SymbolKind.TableExtension,
                EnumProvider.SymbolKind.XmlPort);
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

            if (token.Parent is null || !token.Kind.IsKeyword()
                || DataTypeSyntaxKinds.Contains(token.Parent.Kind)
                || token.Parent.Kind == EnumProvider.SyntaxKind.IdentifierName)
                continue;

            var canonicalToken = SyntaxFactory.Token(token.Kind);
            if (canonicalToken.Kind == EnumProvider.SyntaxKind.None)
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