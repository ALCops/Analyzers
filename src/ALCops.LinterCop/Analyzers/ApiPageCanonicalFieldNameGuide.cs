using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ApiPageCanonicalFieldNameGuide : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.ApiPageCanonicalFieldNameGuide);

    private static readonly Dictionary<string, string> DescriptiveNames =
        new Dictionary<string, string>
            {
                { "SystemId", "id" },
                { "Name", "displayName" },
                { "SystemModifiedAt", "lastModifiedDateTime" }
            };

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeFieldNames,
            EnumProvider.SymbolKind.Page);

    private void AnalyzeFieldNames(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IPageTypeSymbol pageTypeSymbol) return;

        if (pageTypeSymbol.PageType != EnumProvider.PageTypeKind.API)
            return;

        IEnumerable<IControlSymbol> pageFields = pageTypeSymbol.FlattenedControls
                                                        .Where(e => e.ControlKind == EnumProvider.ControlKind.Field &&
                                                            e.RelatedFieldSymbol is not null &&
                                                            IsIdentifierValueTextRec(e, ctx));

        foreach (IControlSymbol field in pageFields)
        {
            string? descriptiveName = GetDescriptiveName(field);

            if (!string.IsNullOrEmpty(descriptiveName) && field.Name != descriptiveName)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ApiPageCanonicalFieldNameGuide,
                    field.GetLocation(),
                    descriptiveName));
            }
        }
    }

    private static string? GetDescriptiveName(IControlSymbol field)
    {
        if (field.RelatedFieldSymbol is null)
            return null;

        if (DescriptiveNames.ContainsKey(field.RelatedFieldSymbol.Name))
            return DescriptiveNames[field.RelatedFieldSymbol.Name];

        if (field.RelatedFieldSymbol.Name.Contains("No.")
                && field.Name.Contains("no", StringComparison.OrdinalIgnoreCase)
                && !field.Name.Contains("number", StringComparison.OrdinalIgnoreCase))
            return ReplaceNoWithNumber(field.Name);

        return null;
    }
    private static string ReplaceNoWithNumber(string input)
    {
        input = input.Replace("No", "Number");
        input = input.Replace("no", "number");
        return input;
    }

    private static bool IsIdentifierValueTextRec(IControlSymbol controlSymbol, SymbolAnalysisContext ctx)
    {
        if (controlSymbol.DeclaringSyntaxReference?.GetSyntax(ctx.CancellationToken) is not PageFieldSyntax pageFieldSyntax)
            return false;

        if (pageFieldSyntax.Expression is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
            return false;

        if (memberAccessExpressionSyntax.Expression is not IdentifierNameSyntax identifierNameSyntax)
            return false;

        if (identifierNameSyntax.Identifier.ValueText is null)
            return false;

        return SemanticFacts.IsSameName(identifierNameSyntax.Identifier.ValueText, "Rec");
    }
}