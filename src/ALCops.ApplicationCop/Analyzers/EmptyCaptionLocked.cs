using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public class EmptyCaptionLocked : DiagnosticAnalyzer
{
    private const string CaptionPropertyName = "Caption";
    private const string LockedPropertyName = "Locked";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.EmptyCaptionLocked);

    // List based on https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/properties/devenv-caption-property
    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            AnalyzeCaptionProperty, new SyntaxKind[] {
            EnumProvider.SyntaxKind.TableObject,
            EnumProvider.SyntaxKind.Field, // TableField
            EnumProvider.SyntaxKind.PageField,
            EnumProvider.SyntaxKind.PageGroup,
            EnumProvider.SyntaxKind.PageObject,
            EnumProvider.SyntaxKind.RequestPage,
            EnumProvider.SyntaxKind.PageLabel,
            EnumProvider.SyntaxKind.PageGroup,
            EnumProvider.SyntaxKind.PagePart,
            EnumProvider.SyntaxKind.PageSystemPart,
            EnumProvider.SyntaxKind.PageAction,
            EnumProvider.SyntaxKind.PageActionSeparator,
            EnumProvider.SyntaxKind.PageActionGroup,
            EnumProvider.SyntaxKind.XmlPortObject,
            EnumProvider.SyntaxKind.ReportObject,
            EnumProvider.SyntaxKind.QueryObject,
            EnumProvider.SyntaxKind.QueryColumn,
            EnumProvider.SyntaxKind.QueryFilter,
            EnumProvider.SyntaxKind.ReportColumn,
            EnumProvider.SyntaxKind.EnumValue,
            EnumProvider.SyntaxKind.PageCustomAction,
            EnumProvider.SyntaxKind.PageSystemAction,
            EnumProvider.SyntaxKind.PageView,
            EnumProvider.SyntaxKind.ReportLayout,
            EnumProvider.SyntaxKind.ProfileObject,
            EnumProvider.SyntaxKind.EnumType,
            EnumProvider.SyntaxKind.PermissionSet,
            EnumProvider.SyntaxKind.TableExtensionObject,
            EnumProvider.SyntaxKind.PageExtensionObject
    });

    private static void AnalyzeCaptionProperty(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        // Prevent double raising the rule on EnumValue in a EnumObject
        if (ctx.Node.IsKind(EnumProvider.SyntaxKind.EnumValue) && ctx.ContainingSymbol.Kind == EnumProvider.SymbolKind.Enum)
            return;

        if (ctx.Node.GetPropertyValue(CaptionPropertyName) is not LabelPropertyValueSyntax captionProperty)
            return;

        var captionText = captionProperty.Value.LabelText.GetLiteralValue()?.ToString();
        if (!string.IsNullOrWhiteSpace(captionText))
            return;

        var lockedNode = captionProperty.Value.Properties?
                                    .DescendantNodes()
                                    .OfType<IdentifierEqualsLiteralSyntax>()
                                    .FirstOrDefault(prop => prop.Identifier.ValueText?.Equals(LockedPropertyName, StringComparison.Ordinal) == true);

        // Check if true is applied for the Locked property
        var isLocked = lockedNode is not null &&
            lockedNode.DescendantNodes()
                        .OfType<BooleanLiteralValueSyntax>()
                        .Any(b => b.Value.IsKind(EnumProvider.SyntaxKind.TrueKeyword));

        if (isLocked)
            return;

        var location = captionProperty.Parent?.GetFirstToken().GetLocation() ?? captionProperty.GetLocation();

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.EmptyCaptionLocked,
            location));
    }
}