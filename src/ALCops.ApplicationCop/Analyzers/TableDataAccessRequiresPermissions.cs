using System.Collections.Immutable;
using ALCops.Common.Permissions;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public class TableDataAccessRequiresPermissions : DiagnosticAnalyzer
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
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        if (invocation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        var operation = MethodOperationMap.GetOperation(invocation.TargetMethod.Name);
        if (operation == DatabaseOperation.None)
            return;

        // Resolve the record type: either from the instance (explicit call) or from the containing table (implicit self-call)
        IRecordTypeSymbol? recordType;
        ITableTypeSymbol? tableType;

        if (invocation.Instance is not null)
        {
            // Explicit call: MyTable.Modify()
            recordType = invocation.Instance.Type as IRecordTypeSymbol;
        }
        else
        {
            // Implicit self-call: Modify() or this.Modify() inside a table object
            recordType = ctx.ContainingSymbol.ContainingType as IRecordTypeSymbol;
        }

        if (recordType is null || recordType.Temporary)
            return;

        tableType = recordType.OriginalDefinition as ITableTypeSymbol;
        if (tableType is null)
            return;

        if (IsSystemTable(tableType))
            return;

        var containingObject = ctx.ContainingSymbol.GetContainingApplicationObjectTypeSymbol();

        if (IsTestCodeunitWithPermissionsDisabled(containingObject))
            return;

        var pageContext = PermissionResolver.GetPageContext(containingObject);
        var containingMethod = ctx.ContainingSymbol as IMethodSymbol;

        var required = new RequiredPermission(tableType, recordType, operation, invocation.Syntax.GetLocation());

        if (PermissionResolver.IsCovered(required, containingObject, containingMethod, pageContext))
            return;

        ReportDiagnostic(ctx.ReportDiagnostic, required);
    }

    private void AnalyzeReportDataItem(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Symbol.GetBooleanPropertyValue(EnumProvider.PropertyKind.UseTemporary) is true)
            return;

        if (ctx.Symbol is not IReportDataItemSymbol reportDataItem)
            return;

        if (reportDataItem.GetTypeSymbol() is not IRecordTypeSymbol recordType)
            return;

        if (recordType.Temporary)
            return;

        if (recordType.OriginalDefinition is not ITableTypeSymbol tableType || IsSystemTable(tableType))
            return;

        var containingObject = ctx.Symbol.GetContainingApplicationObjectTypeSymbol();

        var required = new RequiredPermission(tableType, recordType, DatabaseOperation.Read, ctx.Symbol.GetLocation());

        if (PermissionResolver.IsCovered(required, containingObject, containingMethod: null, pageContext: null))
            return;

        ReportDiagnostic(ctx.ReportDiagnostic, required);
    }

    private void AnalyzeQueryDataItem(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        var targetSymbol = ((IQueryDataItemSymbol)ctx.Symbol).GetTypeSymbol();
        if (targetSymbol.OriginalDefinition is not ITableTypeSymbol tableType || IsSystemTable(tableType))
            return;

        var containingObject = ctx.Symbol.GetContainingApplicationObjectTypeSymbol();

        var required = new RequiredPermission(tableType, targetSymbol, DatabaseOperation.Read, ctx.Symbol.GetLocation());

        if (PermissionResolver.IsCovered(required, containingObject, containingMethod: null, pageContext: null))
            return;

        ReportDiagnostic(ctx.ReportDiagnostic, required);
    }

    private void AnalyzeXmlPortNode(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        var nodeSymbol = (IXmlPortNodeSymbol)ctx.Symbol.OriginalDefinition;
        if (nodeSymbol.SourceTypeKind != EnumProvider.XmlPortSourceTypeKind.Table)
            return;

        var targetSymbol = nodeSymbol.GetTypeSymbol();
        if (targetSymbol.OriginalDefinition is not ITableTypeSymbol tableType || IsSystemTable(tableType))
            return;

        var xmlPort = (IXmlPortTypeSymbol)ctx.Symbol.GetContainingObjectTypeSymbol();
        var containingObject = xmlPort as IApplicationObjectTypeSymbol;

        var direction = ResolveXmlPortDirection(xmlPort);
        var autoReplace = GetXmlPortNodeBoolProperty(ctx.Symbol, EnumProvider.PropertyKind.AutoReplace) ?? true;
        var autoUpdate = GetXmlPortNodeBoolProperty(ctx.Symbol, EnumProvider.PropertyKind.AutoUpdate) ?? true;
        var autoSave = GetXmlPortNodeBoolProperty(ctx.Symbol, EnumProvider.PropertyKind.AutoSave) ?? true;

        var location = ctx.Symbol.GetLocation();

        if (direction == EnumProvider.DirectionKind.Import || direction == EnumProvider.DirectionKind.Both)
        {
            if (autoReplace || autoUpdate)
                CheckAndReport(containingObject, targetSymbol, tableType, DatabaseOperation.Modify, location, ctx.ReportDiagnostic);
            if (autoSave)
                CheckAndReport(containingObject, targetSymbol, tableType, DatabaseOperation.Insert, location, ctx.ReportDiagnostic);
        }

        if (direction == EnumProvider.DirectionKind.Export || direction == EnumProvider.DirectionKind.Both)
            CheckAndReport(containingObject, targetSymbol, tableType, DatabaseOperation.Read, location, ctx.ReportDiagnostic);
    }

    private static void CheckAndReport(
        IApplicationObjectTypeSymbol? containingObject,
        ITypeSymbol variableType,
        ITableTypeSymbol tableType,
        DatabaseOperation operation,
        Microsoft.Dynamics.Nav.CodeAnalysis.Text.Location location,
        Action<Diagnostic> reportDiagnostic)
    {
        var required = new RequiredPermission(tableType, variableType, operation, location);

        if (PermissionResolver.IsCovered(required, containingObject, containingMethod: null, pageContext: null))
            return;

        ReportDiagnostic(reportDiagnostic, required);
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

    private static bool IsSystemTable(ITableTypeSymbol table) => table.Id > 2000000000;

    private static bool IsTestCodeunitWithPermissionsDisabled(IApplicationObjectTypeSymbol? containingObject)
    {
        if (containingObject is not ICodeunitTypeSymbol codeunit)
            return false;

        var subtype = codeunit.GetEnumPropertyValue<CodeunitSubtypeKind>(EnumProvider.PropertyKind.Subtype);
        if (subtype is null || subtype != EnumProvider.CodeunitSubtypeKind.Test)
            return false;

        var testPermissions = codeunit.GetEnumPropertyValue<TestPermissionsKind>(EnumProvider.PropertyKind.TestPermissions);
        return testPermissions is not null && testPermissions == EnumProvider.TestPermissionsKind.Disabled;
    }

    private static DirectionKind ResolveXmlPortDirection(IXmlPortTypeSymbol xmlPort)
    {
        var direction = xmlPort.GetEnumPropertyValue<DirectionKind>(EnumProvider.PropertyKind.Direction);
        return direction ?? EnumProvider.DirectionKind.Both;
    }

    private static bool? GetXmlPortNodeBoolProperty(ISymbol nodeSymbol, PropertyKind propertyKind)
    {
        return (bool?)nodeSymbol.Properties
            .FirstOrDefault(p => p.PropertyKind == propertyKind)?.Value;
    }
}
