using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PageRecordMethodRequiresSourceTable : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.PageRecordMethodRequiresSourceTable);

    private static readonly ImmutableHashSet<string> PageRecordMethodNames =
        ImmutableHashSet.Create(SemanticFacts.NameEqualityComparer,
            "GetRecord",
            "SetRecord",
            "SetSelectionFilter",
            "SetTableView");

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.Operation is not IInvocationExpression operation)
            return;

        var targetMethod = operation.TargetMethod;
        if (targetMethod is null || targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        if (targetMethod.ContainingType?.GetTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Page)
            return;

        if (!PageRecordMethodNames.Contains(targetMethod.Name))
            return;

        if (operation.Arguments.Length != 1)
            return;

        if (!TryResolvePageTypeFromInvocation(operation, out var pageType) || pageType is null)
            return;

        if (pageType.RelatedTable is not null)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PageRecordMethodRequiresSourceTable,
            operation.Syntax.GetLocation(),
            GetPageDisplayName(pageType),
            targetMethod.Name));
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

            pageType = symbol.GetPageTypeSymbol();
            if (pageType is null)
                continue;

            return true;
        }

        return false;
    }

    private static string GetPageDisplayName(IPageTypeSymbol page)
    {
        var ns = page.ContainingNamespace?.QualifiedName ?? string.Empty;
        var name = page.Name.QuoteIdentifierIfNeededWithReflection();

        return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
    }
}