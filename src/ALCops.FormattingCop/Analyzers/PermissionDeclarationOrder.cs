using System.Collections.Immutable;
using ALCops.Common.Permissions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.FormattingCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PermissionDeclarationOrder : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.PermissionDeclarationOrder);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private void AnalyzeCompilation(CompilationAnalysisContext ctx)
    {
        var compilation = ctx.Compilation;

        foreach (var obj in compilation.GetDeclaredApplicationObjectSymbols())
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            var permissionsProperty = obj.GetProperty(EnumProvider.PropertyKind.Permissions);
            if (permissionsProperty is null)
                continue;

            var permissionsSyntax = permissionsProperty.GetPropertyValueSyntax<PermissionPropertyValueSyntax>();
            if (permissionsSyntax is null)
                continue;

            var declaredEntries = permissionsSyntax.PermissionProperties;
            if (declaredEntries.Count <= 1)
                continue;

            if (permissionsSyntax.Parent is not PropertySyntax propertySyntax)
                continue;

            // Mirrors AZ AL Dev Tools: sort within #region groups; leave lists with other
            // directives or unbalanced regions alone.
            if (!PermissionSyntaxHelper.TryBuildRegionTree(propertySyntax, out var root))
                continue;

            if (!PermissionSyntaxHelper.NeedsReordering(root))
                continue;

            // Report on the PropertySyntax so the CodeFix can find it
            var location = propertySyntax.GetLocation();

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PermissionDeclarationOrder,
                location));
        }
    }
}
