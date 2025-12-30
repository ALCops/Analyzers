using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PermissionSetCaptionLength : DiagnosticAnalyzer
{
    private const int MaxCaptionLength = 30;
    private const string LockedPropertyName = "Locked";
    private const string MaxLengthPropertyName = "MaxLength";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.PermissionSetCaptionLength);

    public override void Initialize(AnalysisContext context)
        => context.RegisterSymbolAction(
                CheckPermissionSetNameAndCaptionLength,
                EnumProvider.SymbolKind.PermissionSet);

    private void CheckPermissionSetNameAndCaptionLength(SymbolAnalysisContext ctx)
    {
        var captionProperty = ctx.Symbol.GetProperty(EnumProvider.PropertyKind.Caption);
        if (captionProperty is null)
            return;

        if (captionProperty.ValueText.Length > MaxCaptionLength)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.PermissionSetCaptionLength,
                captionProperty.GetLocation(),
                MaxCaptionLength));
            return;
        }

        var subProperties = ExtractSubProperties(captionProperty);
        if (subProperties is null)
        {
            if (captionProperty is not null)
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.PermissionSetCaptionLength,
                    captionProperty.GetLocation(),
                    MaxCaptionLength));
            return;
        }

        // Check if property "Locked = true" is applied
        var lockedNode = subProperties.OfType<IdentifierEqualsLiteralSyntax>()
                                    .FirstOrDefault(prop => prop.Identifier.ValueText?.Equals(LockedPropertyName, StringComparison.Ordinal) == true);
        var isLocked = lockedNode is not null &&
            lockedNode.DescendantNodes()
                        .OfType<BooleanLiteralValueSyntax>()
                        .Any(b => b.Value.IsKind(EnumProvider.SyntaxKind.TrueKeyword));

        if (isLocked)
            return;

        // Check MaxLength is set to the MaxCaptionLength of 30 (or less)
        var maxLengthNode = subProperties.FirstOrDefault(node => node.ToString().Contains(MaxLengthPropertyName, StringComparison.OrdinalIgnoreCase));
        if (maxLengthNode is not null &&
            int.TryParse(maxLengthNode.DescendantNodes()?
                        .OfType<Int32SignedLiteralValueSyntax>()?
                        .FirstOrDefault()?.Number.ValueText, out int maxLength))
        {
            if (maxLength <= MaxCaptionLength)
                return;
        }

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PermissionSetCaptionLength,
            captionProperty.GetLocation(),
            MaxCaptionLength));
    }

    private IEnumerable<SyntaxNode> ExtractSubProperties(IPropertySymbol? captionProperty)
    {
        var syntaxReference = captionProperty?.DeclaringSyntaxReference;
        if (syntaxReference is null)
            return Enumerable.Empty<SyntaxNode>();

        var syntaxNode = syntaxReference.GetSyntax();
        if (syntaxNode is null)
            return Enumerable.Empty<SyntaxNode>();

        var subPropertyNode = syntaxNode.DescendantNodes()
            .FirstOrDefault(e => e.Kind == EnumProvider.SyntaxKind.CommaSeparatedIdentifierEqualsLiteralList);

        return subPropertyNode?.DescendantNodes() ?? Enumerable.Empty<SyntaxNode>();
    }
}