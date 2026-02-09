using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public class TableDataAccessRequiresPermissions : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.TableDataAccessRequiresPermissions);

    private static readonly ImmutableDictionary<string, char> MethodPermissionMap =
        ImmutableDictionary.CreateRange(
            StringComparer.OrdinalIgnoreCase,
            [
                // read
                new KeyValuePair<string, char>("Find", 'r'),
                new KeyValuePair<string, char>("FindFirst", 'r'),
                new KeyValuePair<string, char>("FindLast", 'r'),
                new KeyValuePair<string, char>("FindSet", 'r'),
                new KeyValuePair<string, char>("Get", 'r'),
                new KeyValuePair<string, char>("IsEmpty", 'r'),

                // insert
                new KeyValuePair<string, char>("Insert", 'i'),

                // modify
                new KeyValuePair<string, char>("Modify", 'm'),
                new KeyValuePair<string, char>("ModifyAll", 'm'),
                new KeyValuePair<string, char>("Rename", 'm'),

                // delete
                new KeyValuePair<string, char>("Delete", 'd'),
                new KeyValuePair<string, char>("DeleteAll", 'd'),
            ]);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);

        context.RegisterSymbolAction(
            CheckReportDataItemObjectPermission,
            EnumProvider.SymbolKind.ReportDataItem);

        context.RegisterSymbolAction(
            CheckQueryDataItemObjectPermission,
            EnumProvider.SymbolKind.QueryDataItem);

        context.RegisterSymbolAction(
            CheckXmlportNodeObjectPermission,
            EnumProvider.SymbolKind.XmlPortNode);
    }

    private void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        if (invocation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        if (invocation.Instance?.Type is not IRecordTypeSymbol recordType || recordType.Temporary)
            return;

        if (recordType.OriginalDefinition is not ITableTypeSymbol tableType)
            return;

        if (TargetTableIsPageSourceTable(ctx, tableType))
            return;

        var permission = GetRequiredPermission(invocation.TargetMethod.Name);
        if (permission is null)
            return;

        var inherentPermissions = GetInherentPermissionsAttributes(ctx);
        if (ProcedureHasInherentPermission(inherentPermissions, recordType, permission.Value))
            return;

        var objectPermissions = ctx.ContainingSymbol
                                    .GetContainingApplicationObjectTypeSymbol()
                                    ?.GetProperty(EnumProvider.PropertyKind.Permissions);

        CheckProcedureInvocation(
            objectPermissions,
            recordType,
            permission.Value,
            ctx.ReportDiagnostic,
            invocation.Syntax.GetLocation(),
            tableType);
    }

    private void CheckXmlportNodeObjectPermission(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (((IXmlPortNodeSymbol)ctx.Symbol.OriginalDefinition).SourceTypeKind != EnumProvider.XmlPortSourceTypeKind.Table) return;

        string direction = "";

        IXmlPortTypeSymbol xmlPort = (IXmlPortTypeSymbol)ctx.Symbol.GetContainingObjectTypeSymbol();

        IPropertySymbol? objectPermissions = xmlPort.GetProperty(EnumProvider.PropertyKind.Permissions);
        ITypeSymbol targetSymbol = ((IXmlPortNodeSymbol)ctx.Symbol.OriginalDefinition).GetTypeSymbol();
        var directionProperty = xmlPort.Properties.FirstOrDefault(property => property.Name == "Direction");

        if (directionProperty is null)
            direction = EnumProvider.DirectionKind.Both.ToString();
        else
            direction = directionProperty.ValueText;

        bool? AutoReplace = (bool?)ctx.Symbol.Properties.FirstOrDefault(property => property.PropertyKind == EnumProvider.PropertyKind.AutoReplace)?.Value; // modify permissions
        bool? AutoUpdate = (bool?)ctx.Symbol.Properties.FirstOrDefault(property => property.PropertyKind == EnumProvider.PropertyKind.AutoUpdate)?.Value; // modify permissions
        bool? AutoSave = (bool?)ctx.Symbol.Properties.FirstOrDefault(property => property.PropertyKind == EnumProvider.PropertyKind.AutoSave)?.Value; // insert permissions

        AutoReplace ??= true;
        AutoUpdate ??= true;
        AutoSave ??= true;

        direction = direction.ToLowerInvariant();

        if (direction == "import" || direction == "both")
        {
            if (AutoReplace == true || AutoUpdate == true)
                CheckProcedureInvocation(objectPermissions, targetSymbol, 'm', ctx.ReportDiagnostic, ctx.Symbol.GetLocation(), (ITableTypeSymbol)targetSymbol.OriginalDefinition);
            if (AutoSave == true)
                CheckProcedureInvocation(objectPermissions, targetSymbol, 'i', ctx.ReportDiagnostic, ctx.Symbol.GetLocation(), (ITableTypeSymbol)targetSymbol.OriginalDefinition);
        }
        if (direction == "export" || direction == "both")
            CheckProcedureInvocation(objectPermissions, targetSymbol, 'r', ctx.ReportDiagnostic, ctx.Symbol.GetLocation(), (ITableTypeSymbol)targetSymbol.OriginalDefinition);
    }

    private void CheckQueryDataItemObjectPermission(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete()) return;

        IPropertySymbol? objectPermissions = ctx.Symbol.GetContainingApplicationObjectTypeSymbol()?.GetProperty(EnumProvider.PropertyKind.Permissions);
        ITypeSymbol targetSymbol = ((IQueryDataItemSymbol)ctx.Symbol).GetTypeSymbol();
        CheckProcedureInvocation(objectPermissions, targetSymbol, 'r', ctx.ReportDiagnostic, ctx.Symbol.GetLocation(), (ITableTypeSymbol)targetSymbol.OriginalDefinition);
    }

    private void CheckReportDataItemObjectPermission(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Symbol.GetBooleanPropertyValue(EnumProvider.PropertyKind.UseTemporary) is true)
            return;

        if (ctx.Symbol is not IReportDataItemSymbol reportDataItemSymbol)
            return;

        if (reportDataItemSymbol.GetTypeSymbol() is not IRecordTypeSymbol recordType)
            return;

        if (recordType.Temporary)
            return;

        var objectPermissions = ctx.Symbol.GetContainingApplicationObjectTypeSymbol()?.GetProperty(EnumProvider.PropertyKind.Permissions);

        CheckProcedureInvocation(
            objectPermissions,
            recordType,
            'r',
            ctx.ReportDiagnostic,
            ctx.Symbol.GetLocation(),
            (ITableTypeSymbol)recordType.OriginalDefinition);
    }

    private static bool ProcedureHasInherentPermission(IEnumerable<IAttributeSymbol> inherentPermissions, ITypeSymbol variableType, char requestedPermission)
    {
        //[InherentPermissions(PermissionObjectType::TableData, Database::"SomeTable", 'r'),InherentPermissions(PermissionObjectType::TableData, Database::"SomeOtherTable", 'w')]

        if (inherentPermissions is null || inherentPermissions.Count() == 0) return false;

        foreach (var inherentPermission in inherentPermissions)
        {
            var inherentPermissionAsString = inherentPermission.DeclaringSyntaxReference?.GetSyntax().ToString();

            var permissions = inherentPermissionAsString?.Split(new[] { '[', ']', '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (permissions?[1].Trim() != "PermissionObjectType::TableData") continue;

            var typeAndObjectName = permissions[2].Trim();
            var permissionValue = permissions[3].Trim().Trim(new[] { '\'', ' ' }).ToLowerInvariant();

            var typeParts = typeAndObjectName.Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
            if (typeParts.Length < 2) continue;

            var objectName = typeParts[1].Trim().Trim('"');
            if (objectName.ToLowerInvariant() != variableType.Name.ToLowerInvariant())
                if (objectName.UnquoteIdentifier().ToLowerInvariant() != (variableType.OriginalDefinition.ContainingNamespace?.QualifiedName.ToLowerInvariant() + "." + variableType.Name.ToLowerInvariant()))
                    continue;

            if (permissionValue.Contains(requestedPermission.ToString().ToLowerInvariant()[0]))
            {
                return true;
            }
        }
        return false;
    }

    private static void CheckProcedureInvocation(IPropertySymbol? objectPermissions, ITypeSymbol variableType, char requestedPermission, Action<Diagnostic> ReportDiagnostic, Microsoft.Dynamics.Nav.CodeAnalysis.Text.Location location, ITableTypeSymbol targetTable)
    {
        if (targetTable.Id > 2000000000)
            return;

        if (TableHasInherentPermission(targetTable, requestedPermission))
            return;

        if (objectPermissions is null)
        {
            ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.TableDataAccessRequiresPermissions, location, requestedPermission, variableType.Name));
            return;
        }

        bool permissionContainRequestedObject = false;
        var permissions = objectPermissions.GetPropertyValueSyntax<PermissionPropertyValueSyntax>();
        foreach (var permission in permissions.PermissionProperties)
        {
            if (!permission.ObjectType.IsKind(EnumProvider.SyntaxKind.TableDataKeyword))
                continue; // ensure permission is tabledata

            var identifier = permission.ObjectReference.Identifier;
            switch (identifier.Kind)
            {
                case var _ when identifier.Kind == EnumProvider.SyntaxKind.IdentifierName:
                    string? name = ((IdentifierNameSyntax)identifier).Identifier.ValueText?.UnquoteIdentifier();
                    if (name is not null && name.Equals(variableType.Name, StringComparison.OrdinalIgnoreCase))
                        permissionContainRequestedObject = true;
                    break;
                case var _ when identifier.Kind == EnumProvider.SyntaxKind.ObjectId:
                    int objectId = Convert.ToInt32(((ObjectIdSyntax)identifier).Value.ValueText);
                    if (objectId == targetTable.Id)
                        permissionContainRequestedObject = true;
                    break;
                case var _ when identifier.Kind == EnumProvider.SyntaxKind.QualifiedName:
                    string qualifier = ((QualifiedNameSyntax)identifier).Left.GetText().ToString();
                    string? onlyName = ((QualifiedNameSyntax)identifier).Right.Identifier.ValueText?.UnquoteIdentifier();
                    if (onlyName is not null && qualifier.Equals(variableType.OriginalDefinition.ContainingNamespace?.QualifiedName, StringComparison.OrdinalIgnoreCase) && onlyName.Equals(variableType.Name, StringComparison.OrdinalIgnoreCase))
                        permissionContainRequestedObject = true;
                    break;
            }
            if (permissionContainRequestedObject)
            {
                var permissionsText = permission.Permissions.ValueText;
                if (permissionsText is null || !permissionsText.ToLowerInvariant().Contains(requestedPermission))
                    ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.TableDataAccessRequiresPermissions, location, requestedPermission, variableType.Name));
                break; // analysed the permissions for the requested object, break the foreach loop
            }
        }
        if (!permissionContainRequestedObject)
        {
            ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.TableDataAccessRequiresPermissions, location, requestedPermission, variableType.Name));
        }
    }

    private static bool TableHasInherentPermission(ITableTypeSymbol table, char requestedPermission)
    {
        IPropertySymbol? permissionProperty = table.GetProperty(EnumProvider.PropertyKind.InherentPermissions);
        // InherentPermissions = RIMD;
        char[]? permissions = permissionProperty?.Value.ToString()?.ToLowerInvariant().Split(new[] { '=' }, 2)[0].Trim().ToCharArray();

        if (permissions is not null && permissions.Contains(requestedPermission.ToString().ToLowerInvariant()[0]))
            return true;

        return false;
    }

    private static char? GetRequiredPermission(string methodName)
    {
        return MethodPermissionMap.TryGetValue(methodName, out var p) ? p : null;
    }

    private static bool TargetTableIsPageSourceTable(OperationAnalysisContext ctx, ITableTypeSymbol targetTable)
    {
        IPageBaseTypeSymbol? page = ctx.ContainingSymbol.GetContainingApplicationObjectTypeSymbol() switch
        {
            IPageBaseTypeSymbol p => p,
            IApplicationObjectExtensionTypeSymbol ext => ext.Target?.OriginalDefinition as IPageBaseTypeSymbol,
            _ => null
        };

        if (page is null || page.RelatedTable is null)
            return false;

        return page.RelatedTable.OriginalDefinition.Equals(targetTable);
    }

    private static IEnumerable<IAttributeSymbol> GetInherentPermissionsAttributes(OperationAnalysisContext ctx)
    {
        if (ctx.ContainingSymbol is not IMethodSymbol method)
            return Enumerable.Empty<IAttributeSymbol>();

        return method.Attributes.Where(a => a.AttributeKind == EnumProvider.AttributeKind.InherentPermissions);
    }
}