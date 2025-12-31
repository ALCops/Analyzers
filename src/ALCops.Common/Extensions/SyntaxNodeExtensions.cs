using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Extensions;

public static class SyntaxNodeExtensions
{
    private const string LockedPropertyName = "Locked";

    public static bool HasLockedPropertyValue(this LabelPropertyValueSyntax labelProperty, bool expectedValue) =>
        labelProperty.Value.Properties.HasLockedPropertyValue(expectedValue);

    public static bool HasLockedPropertyValue(this SyntaxNode node, bool expectedValue)
    {
        if (node is not CommaSeparatedIdentifierEqualsLiteralListSyntax syntaxNode)
            return false;

        var lockedNode =
            syntaxNode
                .DescendantNodes()
                .OfType<IdentifierEqualsLiteralSyntax>()
                .FirstOrDefault(prop =>
                    prop.Identifier.ValueText?.Equals(LockedPropertyName, StringComparison.OrdinalIgnoreCase) == true);

        if (lockedNode is null)
            return false;

        return lockedNode.DescendantNodes()
            .OfType<BooleanLiteralValueSyntax>()
            .Any(b =>
                expectedValue
                    ? b.Value.IsKind(EnumProvider.SyntaxKind.TrueKeyword)
                    : b.Value.IsKind(EnumProvider.SyntaxKind.FalseKeyword));
    }
}