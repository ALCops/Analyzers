using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.ApplicationCop.Analyzer;

[DiagnosticAnalyzer]
public class FlowFieldsShouldNotBeEditable : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.FlowFieldsShouldNotBeEditable);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(new Action<SymbolAnalysisContext>(this.AnalyzeFlowFieldEditable), EnumProvider.SymbolKind.Field);

    private void AnalyzeFlowFieldEditable(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IFieldSymbol field)
        {
            return;
        }

        if (field.FieldClass != EnumProvider.FieldClassKind.FlowField)
        {
            return;
        }

        var editableProperty = field.GetProperty(EnumProvider.PropertyKind.Editable);
        if (editableProperty?.Value is false)
        {
            return;
        }

        if (editableProperty is not null)
        {
            var fieldClassProperty = editableProperty.DeclaringSyntaxReference?.GetSyntax(ctx.CancellationToken).Parent;
            if (fieldClassProperty is not null && HasLineComment(fieldClassProperty.GetTrailingTrivia()))
            {
                return;
            }
        }

        ctx.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.FlowFieldsShouldNotBeEditable,
                field.GetLocation()));
    }

    private static bool HasLineComment(SyntaxTriviaList triviaList)
    {
        return triviaList.Any(trivia => trivia.IsKind(EnumProvider.SyntaxKind.LineCommentTrivia));
    }
}