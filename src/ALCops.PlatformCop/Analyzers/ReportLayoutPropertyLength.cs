using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ReportLayoutPropertyLength : DiagnosticAnalyzer
{
    private const int MaxLength = 250;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.ReportLayoutPropertyLength);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            AnalyzeReportLayout,
            EnumProvider.SyntaxKind.ReportLayout);

    private static void AnalyzeReportLayout(SyntaxNodeAnalysisContext ctx)
    {
        CheckProperty(ctx, EnumProvider.PropertyKind.Caption);
        CheckProperty(ctx, EnumProvider.PropertyKind.Summary);
    }

    private static void CheckProperty(SyntaxNodeAnalysisContext ctx, PropertyKind propertyKind)
    {
        if (ctx.Node.GetPropertyValue(propertyKind) is not LabelPropertyValueSyntax labelProperty)
            return;

        var text = labelProperty.Value.LabelText.GetLiteralValue()?.ToString();
        if (text is null || text.Length <= MaxLength)
            return;

        var location = labelProperty.Parent?.GetFirstToken().GetLocation()
            ?? labelProperty.GetLocation();

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ReportLayoutPropertyLength,
            location,
            propertyKind.ToString(),
            text.Length));
    }
}
