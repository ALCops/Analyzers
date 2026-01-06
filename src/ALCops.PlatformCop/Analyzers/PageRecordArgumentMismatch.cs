using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PageRecordArgumentMismatch : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.PageRecordArgumentMismatch);

    private static readonly ImmutableHashSet<string> PageProcedureNames =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase,
            "GetRecord",
            "SetRecord",
            "SetSelectionFilter",
            "SetTableView");

    private static readonly ImmutableHashSet<string> PageRunProcedureNames =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase,
            "Run",
            "RunModal");

    private static readonly ImmutableArray<PropertyKind> ReferencePageProviders =
        ImmutableArray.Create(
            EnumProvider.PropertyKind.LookupPageId,
            EnumProvider.PropertyKind.DrillDownPageId);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);

        context.RegisterSymbolAction(
            AnalyzeTableReferencePageProvider,
            EnumProvider.SymbolKind.Table);

        context.RegisterSymbolAction(
            AnalyzeTableExtensionReferencePageProvider,
            EnumProvider.SymbolKind.TableExtension);
    }

    private void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.Operation is not IInvocationExpression operation)
            return;

        var targetMethod = operation.TargetMethod;
        if (targetMethod is null || targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        if (targetMethod.ContainingType?.GetTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Page)
            return;

        var methodName = targetMethod.Name;
        if (PageRunProcedureNames.Contains(methodName))
        {
            AnalyzePageRunInvocation(ctx, operation);
            return;
        }

        if (PageProcedureNames.Contains(methodName))
        {
            AnalyzePageVariableInvocation(ctx, operation);
            return;
        }
    }

    private static void AnalyzePageRunInvocation(OperationAnalysisContext ctx, IInvocationExpression operation)
    {
        if (operation.Arguments.Length < 2)
            return;

        var pageArgument = operation.Arguments[0];
        var recordArgument = operation.Arguments[1];

        if (pageArgument.Syntax.Kind != EnumProvider.SyntaxKind.OptionAccessExpression)
            return;

        if (recordArgument.Value.Kind != EnumProvider.OperationKind.ConversionExpression)
            return;

        if (!TryGetPageSourceTableFromPageArgument(pageArgument.Value, out var expectedTable) || expectedTable is null)
            return;

        if (!TryGetRecordBaseTableFromRecordArgument(recordArgument.Value, out var actualBaseTable, out var actualTypeDisplay) || actualBaseTable is null)
            return;

        if (AreSameTable(expectedTable, actualBaseTable))
            return;

        ReportMismatch(
            ctx,
            operation.Syntax.GetLocation(),
            argumentIndex: 2,
            actualTypeDisplay: actualTypeDisplay,
            expectedTable: expectedTable);
    }

    private static void AnalyzePageVariableInvocation(OperationAnalysisContext ctx, IInvocationExpression operation)
    {
        if (operation.Arguments.Length != 1)
            return;

        var recordArgument = operation.Arguments[0];
        if (recordArgument.Value.Kind != EnumProvider.OperationKind.ConversionExpression)
            return;

        if (!TryResolvePageTypeFromInvocation(operation, out var pageType) || pageType is null)
            return;

        var expectedTable = pageType.RelatedTable;
        if (expectedTable is null)
            return;

        if (!TryGetRecordBaseTableFromRecordArgument(recordArgument.Value, out var actualBaseTable, out var actualTypeDisplay) || actualBaseTable is null)
            return;

        if (AreSameTable(expectedTable, actualBaseTable))
            return;

        ReportMismatch(
            ctx,
            operation.Syntax.GetLocation(),
            argumentIndex: 1,
            actualTypeDisplay: actualTypeDisplay,
            expectedTable: expectedTable);
    }

    private void AnalyzeTableReferencePageProvider(SymbolAnalysisContext ctx)
    {
        if (ctx.Symbol is not ITableTypeSymbol table)
            return;

        AnalyzeReferencePageProviders(
            ctx,
            expectedTable: table,
            getProperty: table.GetProperty);
    }

    private void AnalyzeTableExtensionReferencePageProvider(SymbolAnalysisContext ctx)
    {
        if (ctx.Symbol is not ITableExtensionTypeSymbol tableExtension)
            return;

        if (tableExtension.Target is not ITableTypeSymbol targetTable)
            return;

        AnalyzeReferencePageProviders(
            ctx,
            expectedTable: targetTable,
            getProperty: tableExtension.GetProperty);
    }

    private static void AnalyzeReferencePageProviders(SymbolAnalysisContext ctx, ITableTypeSymbol expectedTable, Func<PropertyKind, IPropertySymbol?> getProperty)
    {
        foreach (var propertyKind in ReferencePageProviders)
        {
            var pageReference = getProperty(propertyKind);
            if (pageReference is null)
                continue;

            if (pageReference.Value is not IPageTypeSymbol page)
                continue;

            var pageSourceTable = page.RelatedTable;
            if (pageSourceTable is null)
                continue;

            if (AreSameTable(expectedTable, pageSourceTable))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PageRecordArgumentMismatch,
                pageReference.GetLocation(),
                1,
                FormatTypeAndName(expectedTable),
                FormatTypeAndName(pageSourceTable)));
        }
    }

    private static void ReportMismatch(OperationAnalysisContext ctx, Microsoft.Dynamics.Nav.CodeAnalysis.Text.Location location, int argumentIndex, string actualTypeDisplay, ITableTypeSymbol expectedTable)
    {
        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PageRecordArgumentMismatch,
            location,
            argumentIndex,
            actualTypeDisplay ?? string.Empty,
            FormatTypeAndName(expectedTable)));
    }

    private static string FormatTypeAndName(ITableTypeSymbol table) =>
        table.GetNavTypeKindSafe().ToString() + ' ' + table.Name.QuoteIdentifierIfNeededWithReflection();

    private static bool TryGetPageSourceTableFromPageArgument(IOperation argOperation, out ITableTypeSymbol? expectedTable)
    {
        expectedTable = null;

        if (argOperation is not IApplicationObjectAccess applicationObjectAccess)
            return false;

        var objTypeSymbol = applicationObjectAccess.ApplicationObjectTypeSymbol;
        if (objTypeSymbol is null || objTypeSymbol.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Page)
            return false;

        if (objTypeSymbol.GetTypeSymbol() is not IPageTypeSymbol pageType)
            return false;

        var relatedTable = pageType.RelatedTable;
        if (relatedTable is null)
            return false;

        expectedTable = relatedTable;
        return true;
    }

    private static bool TryGetRecordBaseTableFromRecordArgument(IOperation argumentValueOperation, out ITableTypeSymbol? baseTable, out string actualTypeDisplay)
    {
        baseTable = null;
        actualTypeDisplay = string.Empty;

        if (argumentValueOperation is not IConversionExpression conversion)
            return false;

        var operand = conversion.Operand;
        if (operand is null)
            return false;

        var type = operand.Type;
        if (type is not IRecordTypeSymbol recordType)
            return false;

        baseTable = recordType.BaseTable;
        actualTypeDisplay = type.ToString() ?? string.Empty;
        return true;
    }

    private static bool TryResolvePageTypeFromInvocation(IInvocationExpression operation, out IPageTypeSymbol? pageType)
    {
        pageType = null;
        foreach (var op in operation.DescendantsAndSelf())
        {
            if (op is null)
                continue;

            if (op.Type is null || op.Type.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Page)
                continue;

            var symbol = op.GetSymbol();
            if (symbol is null)
                continue;

            if (TryResolvePageTypeFromSymbol(symbol, out pageType))
                return true;
        }

        return false;
    }

    private static bool TryResolvePageTypeFromSymbol(ISymbol symbol, out IPageTypeSymbol? pageType)
    {
        pageType = null;

        var original = symbol.OriginalDefinition ?? symbol;
        var typeSymbol = original.GetTypeSymbol();
        if (typeSymbol is null)
            return false;

        typeSymbol = typeSymbol.OriginalDefinition as ITypeSymbol ?? typeSymbol;

        if (typeSymbol is not IPageTypeSymbol page)
            return false;

        pageType = page;
        return true;
    }

    private static bool AreSameTable(ITableTypeSymbol expected, ITableTypeSymbol actual)
    {
        if (expected is ISymbol expSym && actual is ISymbol actSym)
        {
            var exp = expSym.OriginalDefinition ?? expSym;
            var act = actSym.OriginalDefinition ?? actSym;

            if (ReferenceEquals(exp, act))
                return true;

            if (exp.Equals(act))
                return true;
        }

        // Fallback on namespace with name comparison
        var eNs = expected.ContainingNamespace?.QualifiedName ?? string.Empty;
        var aNs = actual.ContainingNamespace?.QualifiedName ?? string.Empty;

        if (!string.Equals(eNs, aNs, StringComparison.Ordinal))
            return false;

        return string.Equals(expected.Name, actual.Name, StringComparison.OrdinalIgnoreCase);
    }
}
