using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.FormattingCop.Analyzer;

[DiagnosticAnalyzer]
public class SemicolonAfterMethodOrTriggerDeclaration : DiagnosticAnalyzer
{
    private const string DeclarationSuffix = "Declaration";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.SemicolonAfterMethodOrTriggerDeclaration);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(new Action<SyntaxNodeAnalysisContext>(this.AnalyzeSemicolonAfterMethodOrTriggerDeclaration),
            EnumProvider.SyntaxKind.MethodDeclaration,
            EnumProvider.SyntaxKind.TriggerDeclaration);

    private void AnalyzeSemicolonAfterMethodOrTriggerDeclaration(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not MethodOrTriggerDeclarationSyntax syntax)
            return;

        if (syntax.Body is null)
            return;

        if (syntax.SemicolonToken.Kind != EnumProvider.SyntaxKind.None)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.SemicolonAfterMethodOrTriggerDeclaration,
                syntax.SemicolonToken.GetLocation(),
                GetMethodOrTriggerName(syntax)));
        }
    }

    private static string GetMethodOrTriggerName(MethodOrTriggerDeclarationSyntax syntax)
    {
        var kind = syntax.Kind.ToString();

        return kind.EndsWith(DeclarationSuffix, StringComparison.Ordinal)
            ? kind.Substring(0, kind.Length - DeclarationSuffix.Length)
            : kind;
    }
}