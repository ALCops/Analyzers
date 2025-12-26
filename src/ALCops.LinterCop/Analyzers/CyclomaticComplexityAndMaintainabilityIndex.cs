using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class CyclomaticComplexityAndMaintainabilityIndex : DiagnosticAnalyzer
{
    private static readonly HashSet<string> EventPublisherDecoratorNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "BusinessEvent",
        "IntegrationEvent",
        "ExternalBusinessEvent"
    };

    private static readonly HashSet<SyntaxKind> OperatorAndOperandKinds =
        Enum.GetValues(typeof(SyntaxKind))
            .Cast<SyntaxKind>()
            .Where(value => value.ToString().Contains("Keyword") ||
                            value.ToString().Contains("Token") ||
                            IsOperandKind(value))
            .ToHashSet();

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.CyclomaticComplexityMetric,
            DiagnosticDescriptors.CyclomaticComplexityThresholdExceeded,
            DiagnosticDescriptors.MaintainabilityIndexMetric,
            DiagnosticDescriptors.MaintainabilityIndexThresholdExceeded);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCodeBlockAction(
            this.Analyze);

    private void Analyze(CodeBlockAnalysisContext context)
    {
        if (context.IsObsolete() || context.CodeBlock is not MethodOrTriggerDeclarationSyntax methodOrTrigger)
            return;

        var containingObjectTypeSymbol = context.OwningSymbol.GetContainingObjectTypeSymbol();
        var navTypeKind = containingObjectTypeSymbol.NavTypeKind;
        if (navTypeKind == EnumProvider.NavTypeKind.Interface ||
            navTypeKind == EnumProvider.NavTypeKind.ControlAddIn)
            return;

        var body = methodOrTrigger.Body;
        if (body is null)
            return;

        if (body.Statements.Count == 0)
        {
            if (methodOrTrigger.Attributes.Count == 0)
                return;

            foreach (var attr in methodOrTrigger.Attributes)
            {
                var name = attr.GetIdentifierOrLiteralValue();
                if (name is not null && EventPublisherDecoratorNames.Contains(name))
                    return;
            }

            return;
        }

        var settings = ALCopsSettingsProvider.GetSettings(
            context.SemanticModel.Compilation.FileSystem?.GetDirectoryPath());

        var descendantNodesAndTokens = methodOrTrigger.Body.DescendantNodesAndTokens(static _ => true);
        var cyclomaticComplexity = CalculateCyclomaticComplexityMetric(context, body, descendantNodesAndTokens);

        if (cyclomaticComplexity >= settings.CyclomaticComplexityThreshold)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.CyclomaticComplexityThresholdExceeded,
                    context.OwningSymbol.GetLocation(),
                    cyclomaticComplexity,
                    settings.CyclomaticComplexityThreshold));
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.CyclomaticComplexityMetric,
                context.OwningSymbol.GetLocation(),
                cyclomaticComplexity,
                settings.CyclomaticComplexityThreshold));

        if (!context.IsDiagnosticEnabled(DiagnosticDescriptors.MaintainabilityIndexMetric) ||
            !context.IsDiagnosticEnabled(DiagnosticDescriptors.MaintainabilityIndexThresholdExceeded))
            return;

        var maintainabilityIndexMetric = Math.Round(CalculateMaintainabilityIndexMetric(context, body, descendantNodesAndTokens, cyclomaticComplexity));
        if (maintainabilityIndexMetric <= settings.MaintainabilityIndexThreshold)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.MaintainabilityIndexThresholdExceeded,
                    context.OwningSymbol.GetLocation(),
                    maintainabilityIndexMetric,
                    settings.MaintainabilityIndexThreshold));
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.MaintainabilityIndexMetric,
                context.OwningSymbol.GetLocation(),
                maintainabilityIndexMetric,
                settings.MaintainabilityIndexThreshold));
    }

    private static int CalculateCyclomaticComplexityMetric(CodeBlockAnalysisContext context, BlockSyntax body, IEnumerable<SyntaxNodeOrToken> descendantNodesAndTokens)
    {
        return descendantNodesAndTokens.Count(syntaxNodeOrToken => IsComplexKind(syntaxNodeOrToken.Kind)) + 1;
    }

    private static double CalculateMaintainabilityIndexMetric(CodeBlockAnalysisContext context, BlockSyntax body, IEnumerable<SyntaxNodeOrToken> descendantNodesAndTokens, int cyclomaticComplexity)
    {
        try
        {
            var triviaLinesCount = body
                .DescendantTrivia(e => true, true)
                .Count(node =>
                    node.Kind == EnumProvider.SyntaxKind.EndOfLineTrivia &&
                    node.GetLocation().GetLineSpan().StartLinePosition.Line ==
                    node.Token.GetLocation().GetLineSpan().StartLinePosition.Line) - 2; //Minus 2 for Begin end of function

            context.CancellationToken.ThrowIfCancellationRequested();
            var N = 0;
            using var hashSet = PooledHashSet<SyntaxNodeOrToken>.GetInstance();
            foreach (var nodeOrToken in descendantNodesAndTokens)
            {
                if (OperatorAndOperandKinds.Contains(nodeOrToken.Kind))
                {
                    N++;
                    hashSet.Add(nodeOrToken);
                }
            }

            double HalsteadVolume = N * Math.Log(hashSet.Count, 2);

            //171−5.2lnV−0.23G−16.2lnL
            return Math.Max(0, (171 - 5.2 * Math.Log(HalsteadVolume) - 0.23 * cyclomaticComplexity - 16.2 * Math.Log(triviaLinesCount)) * 100 / 171);
        }
        catch (System.NullReferenceException)
        {
            return 0.0;
        }
    }

    private static bool IsOperandKind(SyntaxKind kind)
        => kind == EnumProvider.SyntaxKind.IdentifierToken
        || kind == EnumProvider.SyntaxKind.Int32LiteralToken
        || kind == EnumProvider.SyntaxKind.StringLiteralToken
        || kind == EnumProvider.SyntaxKind.BooleanLiteralValue
        || kind == EnumProvider.SyntaxKind.TrueKeyword
        || kind == EnumProvider.SyntaxKind.FalseKeyword;

    private static bool IsComplexKind(SyntaxKind kind)
        => kind == EnumProvider.SyntaxKind.IfKeyword
        || kind == EnumProvider.SyntaxKind.ElifKeyword
        || kind == EnumProvider.SyntaxKind.LogicalAndExpression
        || kind == EnumProvider.SyntaxKind.LogicalOrExpression
        || kind == EnumProvider.SyntaxKind.CaseLine
        || kind == EnumProvider.SyntaxKind.ForKeyword
        || kind == EnumProvider.SyntaxKind.ForEachKeyword
        || kind == EnumProvider.SyntaxKind.WhileKeyword
        || kind == EnumProvider.SyntaxKind.UntilKeyword
        || kind == EnumProvider.SyntaxKind.ConditionalExpression; // Ternary operator
}