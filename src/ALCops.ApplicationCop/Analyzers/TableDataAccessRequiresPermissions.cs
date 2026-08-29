using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Permissions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class TableDataAccessRequiresPermissions : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.TableDataAccessRequiresPermissions);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);

        context.RegisterSymbolAction(
            AnalyzeReportDataItem,
            EnumProvider.SymbolKind.ReportDataItem);

        context.RegisterSymbolAction(
            AnalyzeQueryDataItem,
            EnumProvider.SymbolKind.QueryDataItem);

        context.RegisterSymbolAction(
            AnalyzeXmlPortNode,
            EnumProvider.SymbolKind.XmlPortNode);
    }

    private void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        var containingObject = ctx.ContainingSymbol.GetContainingApplicationObjectTypeSymbol();

        if (containingObject?.Kind == EnumProvider.SymbolKind.PermissionSet
            || containingObject?.Kind == EnumProvider.SymbolKind.PermissionSetExtension
            || containingObject.IsTestCodeunitWithPermissionsDisabled())
            return;

        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        var required = RequiredPermissionDetector.TryGetFromInvocation(invocation, ctx.ContainingSymbol);
        if (required is null)
        {
            AnalyzeDataTransferInvocation(ctx, containingObject, invocation);
            return;
        }

        ReportIfNotCovered(ctx, containingObject, required.Value);
    }

    /// <summary>
    /// A <c>DataTransfer</c> executor takes its tables from <c>SetTables</c>, not the receiver,
    /// so it never resolves through <see cref="RequiredPermissionDetector.TryGetFromInvocation"/>.
    /// Silent when they are unresolvable, rather than guessing a table.
    /// </summary>
    private static void AnalyzeDataTransferInvocation(
        OperationAnalysisContext ctx,
        IApplicationObjectTypeSymbol? containingObject,
        IInvocationExpression invocation)
    {
        if (invocation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod
            || invocation.Instance?.Type?.NavTypeKind != EnumProvider.NavTypeKind.DataTransfer
            || !DataTransferOperations.IsExecutor(invocation.TargetMethod.Name))
            return;

        var semanticModel = ctx.Compilation.GetSemanticModel(invocation.Syntax.SyntaxTree);
        var required = new List<RequiredPermission>();

        if (!RequiredPermissionDetector.TryGetFromDataTransfer(
                invocation, semanticModel, includeSystemTables: false, required, ctx.CancellationToken))
            return;

        foreach (var permission in required)
            ReportIfNotCovered(ctx, containingObject, permission);
    }

    private static void ReportIfNotCovered(
        OperationAnalysisContext ctx,
        IApplicationObjectTypeSymbol? containingObject,
        RequiredPermission required)
    {
        var pageContext = PermissionResolver.GetPageContext(containingObject);
        var containingMethod = ctx.ContainingSymbol as IMethodSymbol;

        if (PermissionResolver.IsCovered(required, containingObject, containingMethod, pageContext))
            return;

        ReportDiagnostic(ctx.ReportDiagnostic, required);
    }

    private void AnalyzeReportDataItem(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        var required = RequiredPermissionDetector.TryGetFromReportDataItem(ctx.Symbol);
        if (required is null)
            return;

        var containingObject = ctx.Symbol.GetContainingApplicationObjectTypeSymbol();

        if (PermissionResolver.IsCovered(required.Value, containingObject, containingMethod: null, pageContext: null))
            return;

        ReportDiagnostic(ctx.ReportDiagnostic, required.Value);
    }

    private void AnalyzeQueryDataItem(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        var required = RequiredPermissionDetector.TryGetFromQueryDataItem(ctx.Symbol);
        if (required is null)
            return;

        var containingObject = ctx.Symbol.GetContainingApplicationObjectTypeSymbol();

        if (PermissionResolver.IsCovered(required.Value, containingObject, containingMethod: null, pageContext: null))
            return;

        ReportDiagnostic(ctx.ReportDiagnostic, required.Value);
    }

    private void AnalyzeXmlPortNode(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        var containingObject = ctx.Symbol.GetContainingApplicationObjectTypeSymbol();

        foreach (var required in RequiredPermissionDetector.GetFromXmlPortNode(ctx.Symbol))
        {
            if (PermissionResolver.IsCovered(required, containingObject, containingMethod: null, pageContext: null))
                continue;

            ReportDiagnostic(ctx.ReportDiagnostic, required);
        }
    }

    private static void ReportDiagnostic(Action<Diagnostic> report, RequiredPermission required)
    {
        var permissionChar = MethodOperationMap.ToPermissionChar(required.Operation);
        var tableNamespace = required.VariableType.GetContainingNamespaceQualifiedNameWithReflection() ?? string.Empty;

        var properties = ImmutableDictionary<string, string>.Empty
            .Add("TableName", required.VariableType.Name)
            .Add("TableNamespace", tableNamespace)
            .Add("PermissionChar", permissionChar.ToString());

        report(Diagnostic.Create(
            DiagnosticDescriptors.TableDataAccessRequiresPermissions,
            required.Location,
            properties,
            permissionChar,
            required.VariableType.Name));
    }
}
