using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class LabelTokLockedConvention : DiagnosticAnalyzer
{
    private const string TokSuffix = "Tok";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.LabelLockedMustHaveTokSuffix,
            DiagnosticDescriptors.LabelWithTokSuffixMustBeLocked
        );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeLockedLabel,
            EnumProvider.SymbolKind.GlobalVariable,
            EnumProvider.SymbolKind.LocalVariable
        );

    private void AnalyzeLockedLabel(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IVariableSymbol symbol)
            return;

        if (symbol.Type is not ILabelTypeSymbol type || type.Name is null)
            return;

        bool IsEndsWithTok = type.Name.EndsWith(TokSuffix, StringComparison.Ordinal);
        bool isLocked = type.Locked is true;

        if (isLocked && !IsEndsWithTok)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LabelLockedMustHaveTokSuffix,
                symbol.GetLocation(),
                symbol.Name));
        }

        if (!isLocked && IsEndsWithTok)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LabelWithTokSuffixMustBeLocked,
                symbol.GetLocation(),
                symbol.Name));
        }
    }
}