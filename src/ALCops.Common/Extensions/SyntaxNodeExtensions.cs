using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Extensions;

public static class SyntaxNodeExtensions
{
    public static int? GetIntegerPropertyValue(this LabelPropertyValueSyntax labelProperty, IdentifierProperty property) =>
        labelProperty.Value.Properties.GetIntegerPropertyValue(property);

    public static int? GetIntegerPropertyValue(this SyntaxNode node, IdentifierProperty property)
    {
        // Currently only 'MaxLength' property is supported
        if (property != IdentifierProperty.MaxLength)
            return null;

        if (node is not CommaSeparatedIdentifierEqualsLiteralListSyntax syntaxNode)
            return null;

        var intLiteral = node.FindIdentifierNode(property.ToString())?
                .DescendantNodes()
                .OfType<Int32SignedLiteralValueSyntax>()
                .FirstOrDefault();

        if (intLiteral is null)
            return null;

        if (!int.TryParse(intLiteral.Number.ValueText, out int value))
            return null;

        return value;
    }

    public static bool? GetBooleanPropertyValue(this LabelPropertyValueSyntax labelProperty, IdentifierProperty property) =>
        labelProperty.Value.Properties.GetBooleanPropertyValue(property);

    public static bool? GetBooleanPropertyValue(this SyntaxNode node, IdentifierProperty property)
    {
        // Currently only 'Locked' property is supported
        if (property != IdentifierProperty.Locked)
            return null;

        if (node is not CommaSeparatedIdentifierEqualsLiteralListSyntax syntaxNode)
            return null;

        var boolLiteral = node.FindIdentifierNode(property.ToString())?
                .DescendantNodes()
                .OfType<BooleanLiteralValueSyntax>()
                .FirstOrDefault();

        if (boolLiteral is null)
            return null;

        if (boolLiteral.Value.IsKind(EnumProvider.SyntaxKind.TrueKeyword))
            return true;

        if (boolLiteral.Value.IsKind(EnumProvider.SyntaxKind.FalseKeyword))
            return false;

        return null;
    }

    private static IdentifierEqualsLiteralSyntax? FindIdentifierNode(this SyntaxNode node, string propertyName)
    {
        return node
                .DescendantNodes()
                .OfType<IdentifierEqualsLiteralSyntax>()
                .FirstOrDefault(prop =>
                    prop.Identifier.ValueText?.Equals(propertyName, StringComparison.OrdinalIgnoreCase) == true);
    }
}