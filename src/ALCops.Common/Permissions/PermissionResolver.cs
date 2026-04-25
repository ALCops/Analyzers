using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.Common.Permissions;

/// <summary>
/// Resolves declared permissions from multiple sources:
/// 1. Object-level Permissions property
/// 2. Method-level [InherentPermissions] attribute
/// 3. Table-level InherentPermissions property
/// 4. Page SourceTable exemption
/// </summary>
public static class PermissionResolver
{
    /// <summary>
    /// Checks whether a required permission is covered by any declared permission source.
    /// </summary>
    public static bool IsCovered(
        RequiredPermission required,
        IApplicationObjectTypeSymbol? containingObject,
        IMethodSymbol? containingMethod,
        IPageBaseTypeSymbol? pageContext)
    {
        // 1. Page SourceTable exemption: all CRUD on the page's own source table is exempt
        if (pageContext is not null && IsPageSourceTable(pageContext, required.Table))
            return true;

        // 2. Table-level InherentPermissions property
        if (TableHasInherentPermission(required.Table, required.Operation))
            return true;

        // 3. Method-level [InherentPermissions] attribute
        if (containingMethod is not null && MethodHasInherentPermission(containingMethod, required.VariableType, required.Operation))
            return true;

        // 4. Object-level Permissions property
        if (containingObject is not null)
        {
            var objectPermissions = containingObject.GetProperty(EnumProvider.PropertyKind.Permissions);
            if (ObjectPermissionCovers(objectPermissions, required.VariableType, required.Table, required.Operation))
                return true;
        }

        return false;
    }

    private static bool IsPageSourceTable(IPageBaseTypeSymbol page, ITableTypeSymbol targetTable)
    {
        if (page.RelatedTable is null)
            return false;

        return page.RelatedTable.OriginalDefinition.Equals(targetTable);
    }

    /// <summary>
    /// Resolves the page context for a containing object, including page extensions.
    /// </summary>
    public static IPageBaseTypeSymbol? GetPageContext(IApplicationObjectTypeSymbol? containingObject) =>
        containingObject switch
        {
            IPageBaseTypeSymbol p => p,
            IApplicationObjectExtensionTypeSymbol ext => ext.Target?.OriginalDefinition as IPageBaseTypeSymbol,
            _ => null
        };

    private static bool TableHasInherentPermission(ITableTypeSymbol table, DatabaseOperation operation)
    {
        var permissionProperty = table.GetProperty(EnumProvider.PropertyKind.InherentPermissions);
        if (permissionProperty?.Value is null)
            return false;

        var permissionText = permissionProperty.Value.ToString();
        if (string.IsNullOrEmpty(permissionText))
            return false;

        // InherentPermissions = RIMD; (take only the part before '=' if present)
        var equalsIndex = permissionText.IndexOf('=');
        var permissionChars = equalsIndex >= 0
            ? permissionText.Substring(0, equalsIndex).Trim()
            : permissionText.Trim();

        var requiredChar = MethodOperationMap.ToPermissionChar(operation);
        return permissionChars.IndexOf(requiredChar.ToString(), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool MethodHasInherentPermission(IMethodSymbol method, ITypeSymbol variableType, DatabaseOperation operation)
    {
        var inherentPermissions = method.Attributes
            .Where(a => a.AttributeKind == EnumProvider.AttributeKind.InherentPermissions);

        foreach (var attr in inherentPermissions)
        {
            if (!TryParseInherentPermissionAttribute(attr, out var tableName, out var permissions))
                continue;

            if (!IsTableNameMatch(tableName, variableType))
                continue;

            var requiredChar = MethodOperationMap.ToPermissionChar(operation);
            if (permissions.IndexOf(requiredChar.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Parses an [InherentPermissions] attribute to extract the table name and permission string.
    /// Format: [InherentPermissions(PermissionObjectType::TableData, Database::"SomeTable", 'rimd')]
    /// </summary>
    private static bool TryParseInherentPermissionAttribute(IAttributeSymbol attribute, out string tableName, out string permissions)
    {
        tableName = string.Empty;
        permissions = string.Empty;

        var syntaxRef = attribute.DeclaringSyntaxReference;
        if (syntaxRef is null)
            return false;

        var syntaxText = syntaxRef.GetSyntax().ToString();
        if (string.IsNullOrEmpty(syntaxText))
            return false;

        // Split by comma to get the three arguments
        var parts = syntaxText.Split(new[] { '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
            return false;

        // parts[0] = "InherentPermissions" or the attribute name
        // parts[1] = "PermissionObjectType::TableData"
        // parts[2] = "Database::\"SomeTable\""
        // parts[3] = "'rimd'"

        if (!parts[1].Trim().Equals("PermissionObjectType::TableData", StringComparison.OrdinalIgnoreCase))
            return false;

        var typeAndName = parts[2].Trim();
        var typeParts = typeAndName.Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
        if (typeParts.Length < 2)
            return false;

        tableName = typeParts[1].Trim().Trim('"');
        permissions = parts[3].Trim().Trim('\'', ' ');

        return !string.IsNullOrEmpty(tableName) && !string.IsNullOrEmpty(permissions);
    }

    private static bool IsTableNameMatch(string declaredName, ITypeSymbol variableType)
    {
        // Match by simple name
        if (declaredName.Equals(variableType.Name, StringComparison.OrdinalIgnoreCase))
            return true;

        // Match by unquoted simple name
        if (declaredName.UnquoteIdentifier().Equals(variableType.Name, StringComparison.OrdinalIgnoreCase))
            return true;

        // Match by fully qualified name (namespace.name)
        var qualifiedName = variableType.GetFullyQualifiedObjectName();
        if (declaredName.UnquoteIdentifier().Equals(qualifiedName, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool ObjectPermissionCovers(
        IPropertySymbol? objectPermissions,
        ITypeSymbol variableType,
        ITableTypeSymbol targetTable,
        DatabaseOperation operation)
    {
        if (objectPermissions is null)
            return false;

        var permissionsSyntax = objectPermissions.GetPropertyValueSyntax<PermissionPropertyValueSyntax>();
        if (permissionsSyntax is null)
            return false;

        foreach (var permission in permissionsSyntax.PermissionProperties)
        {
            if (!permission.ObjectType.IsKind(EnumProvider.SyntaxKind.TableDataKeyword))
                continue;

            if (!PermissionMatchesTable(permission.ObjectReference.Identifier, variableType, targetTable))
                continue;

            // Found a matching table entry; check if the operation is covered
            var permissionsText = permission.Permissions.ValueText;
            if (string.IsNullOrEmpty(permissionsText))
                return false;

            var requiredChar = MethodOperationMap.ToPermissionChar(operation);
            return permissionsText.IndexOf(requiredChar.ToString(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        return false;
    }

    /// <summary>
    /// Matches a permission property table reference against a target table.
    /// Handles IdentifierNameSyntax, QualifiedNameSyntax, and ObjectIdSyntax.
    /// </summary>
    private static bool PermissionMatchesTable(SyntaxNode identifier, ITypeSymbol variableType, ITableTypeSymbol targetTable)
    {
        if (identifier.Kind == EnumProvider.SyntaxKind.IdentifierName)
        {
            var name = ((IdentifierNameSyntax)identifier).Identifier.ValueText?.UnquoteIdentifier();
            return name is not null && name.Equals(variableType.Name, StringComparison.OrdinalIgnoreCase);
        }

        if (identifier.Kind == EnumProvider.SyntaxKind.ObjectId)
        {
            if (int.TryParse(((ObjectIdSyntax)identifier).Value.ValueText, out var objectId))
                return objectId == targetTable.Id;
            return false;
        }

        if (identifier.Kind == EnumProvider.SyntaxKind.QualifiedName)
        {
            var qualified = (QualifiedNameSyntax)identifier;
            var qualifier = qualified.Left.GetText().ToString();
            var name = qualified.Right.Identifier.ValueText?.UnquoteIdentifier();

            if (name is null)
                return false;

            // Match namespace.name against the variable's fully qualified name
            var variableNamespace = variableType.OriginalDefinition.GetContainingNamespaceQualifiedNameWithReflection();
            return qualifier.Equals(variableNamespace, StringComparison.OrdinalIgnoreCase)
                && name.Equals(variableType.Name, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
