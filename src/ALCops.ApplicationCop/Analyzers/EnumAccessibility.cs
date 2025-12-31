using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class EnumAccessibility : DiagnosticAnalyzer
{
    private const string CaptionPropertyName = "Caption";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.EnumEmptyValueHasCaption,
            DiagnosticDescriptors.EnumValueHasEmptyCaption
        );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            AnalyzeEnumWithCaption,
            EnumProvider.SyntaxKind.EnumValue
        );

    private void AnalyzeEnumWithCaption(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not EnumValueSyntax enumValue)
            return;

        // Prevent possible duplicate diagnostic
        if (ctx.ContainingSymbol.ContainingType is null)
            return;

        if (ctx.Node.GetPropertyValue(CaptionPropertyName) is not LabelPropertyValueSyntax captionProperty)
            return;

        // Incomplete StringLiteral (just a single starting quote) so skip further analysis
        if (captionProperty.Value.LabelText.Value.Text == "'")
            return;

        string? enumValueName = enumValue.GetNameStringValue();
        string? enumCaptionText = captionProperty.Value.LabelText.Value.Value?.ToString();

        // Empty enum value must not have a caption (unless caption is also empty)
        if (string.IsNullOrEmpty(enumValueName) && !string.IsNullOrEmpty(enumCaptionText))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.EnumEmptyValueHasCaption,
                enumValue.Name.GetLocation()));

            return;
        }

        if (captionProperty.HasLockedPropertyValue(true))
            return;

        // Non-empty enum value must have a non-empty caption
        if (!string.IsNullOrEmpty(enumValueName) && string.IsNullOrEmpty(enumCaptionText))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.EnumValueHasEmptyCaption,
                captionProperty.Value.LabelText.GetLocation(),
                enumValueName));
        }
    }
}