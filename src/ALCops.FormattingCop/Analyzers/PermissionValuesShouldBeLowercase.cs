using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.FormattingCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PermissionValuesShouldBeLowercase : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.PermissionValuesShouldBeLowercase);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            AnalyzePermissionPropertyValue,
            EnumProvider.SyntaxKind.PermissionPropertyValue);

    private static void AnalyzePermissionPropertyValue(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not PermissionPropertyValueSyntax permissionValue)
            return;

        // In permissionset objects the casing of permission values is semantic:
        // uppercase grants direct permissions, lowercase grants indirect permissions.
        if (IsInPermissionSetObject(permissionValue))
            return;

        // AccessByPermission reuses the PermissionPropertyValue node but is a UI-visibility
        // mask, not an indirect-permission grant; uppercase is its documented form (issue #474).
        if (IsAccessByPermissionProperty(permissionValue))
            return;

        if (!HasUppercasePermissionValue(permissionValue))
            return;

        // Report on the PropertySyntax so the CodeFix can find it
        var location = (permissionValue.Parent ?? permissionValue).GetLocation();

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PermissionValuesShouldBeLowercase,
            location));
    }

    private static bool IsAccessByPermissionProperty(PermissionPropertyValueSyntax permissionValue) =>
        permissionValue.Parent is PropertySyntax property &&
        property.Name?.Identifier.ValueText.IsSameName("AccessByPermission") == true;

    private static bool IsInPermissionSetObject(SyntaxNode node)
    {
        foreach (var ancestor in node.Ancestors())
        {
            var kind = ancestor.Kind;
            if (kind == EnumProvider.SyntaxKind.PermissionSet ||
                kind == EnumProvider.SyntaxKind.PermissionSetExtension)
                return true;
        }

        return false;
    }

    internal static bool HasUppercasePermissionValue(PermissionPropertyValueSyntax permissionValue)
    {
        foreach (var permission in permissionValue.PermissionProperties)
        {
            if (ContainsUppercase(permission.Permissions.Text))
                return true;
        }

        return false;
    }

    internal static bool ContainsUppercase(string? permissionValueText)
    {
        if (string.IsNullOrEmpty(permissionValueText))
            return false;

        foreach (var c in permissionValueText)
        {
            if (char.IsUpper(c))
                return true;
        }

        return false;
    }
}
