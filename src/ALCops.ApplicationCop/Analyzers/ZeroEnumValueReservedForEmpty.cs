using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public class ZeroEnumValueReservedForEmpty : DiagnosticAnalyzer
{
    private const string CaptionPropertyName = "Caption";
    private const int ReservedValue = 0;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    ImmutableArray.Create(
        DiagnosticDescriptors.ZeroEnumValueReservedForEmpty
    );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            AnalyzeReservedEnum,
            EnumProvider.SyntaxKind.EnumValue
        );

    private void AnalyzeReservedEnum(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not EnumValueSyntax enumValue)
            return;

        if (ctx.ContainingSymbol.Kind != EnumProvider.SymbolKind.Enum)
            return;

        if (!TryGetEnumValueAsInt(enumValue, out int value) || value != ReservedValue)
            return;

        if (ctx.ContainingSymbol.GetContainingApplicationObjectTypeSymbol() is not IEnumTypeSymbol enumTypeSymbol ||
            enumTypeSymbol.ImplementedInterfaces.Any())
            return;

        if (!string.IsNullOrWhiteSpace(enumValue.GetNameStringValue()))
        {
            Report(ctx, enumValue.Name.GetLocation(), enumValue.Name.Unquoted());
            return;
        }

        if (ctx.Node.GetPropertyValue(CaptionPropertyName) is not LabelPropertyValueSyntax caption)
            return;

        if (!string.IsNullOrWhiteSpace(caption.Value.LabelText.Value.Value.ToString()))
            Report(ctx, caption.GetLocation(), enumValue.Name.Unquoted());
    }

    private static bool TryGetEnumValueAsInt(EnumValueSyntax enumValue, out int value)
    {
        value = default;

        if (enumValue.Id.ValueText is null)
            return false;

        return int.TryParse(enumValue.Id.ValueText, out value);
    }

    private static void Report(SyntaxNodeAnalysisContext ctx, Location location, string name) =>
        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ZeroEnumValueReservedForEmpty,
            location,
            name));
}