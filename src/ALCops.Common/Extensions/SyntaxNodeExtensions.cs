using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Extensions;

public static class SyntaxNodeExtensions
{
    public static int? GetIntegerPropertyValue(this LabelPropertyValueSyntax? labelProperty, IdentifierProperty property) =>
        labelProperty?.Value.GetIntegerPropertyValue(property);

    public static int? GetIntegerPropertyValue(this LabelSyntax? labelProperty, IdentifierProperty property) =>
        labelProperty?.Properties.GetIntegerPropertyValue(property);

    public static int? GetIntegerPropertyValue(this SyntaxNode? node, IdentifierProperty property, bool includeChildNodes = false)
    {
        if (node is null)
            return null;

        var propertyList = node as CommaSeparatedIdentifierEqualsLiteralListSyntax;
        if (propertyList is not null)
            return GetIntegerPropertyValue(propertyList, property);

        if (!includeChildNodes)
            return null;

        var identifierEqualsLiteralList = FindIdentifierEqualsLiteralList(node);
        if (identifierEqualsLiteralList is null)
            return null;

        return GetIntegerPropertyValue(identifierEqualsLiteralList, property);
    }

    public static int? GetIntegerPropertyValue(this CommaSeparatedIdentifierEqualsLiteralListSyntax? node, IdentifierProperty property)
    {
        if (node is null)
            return null;

        // Currently only 'MaxLength' property is supported
        if (property != IdentifierProperty.MaxLength)
            return null;

        var intLiteral = node
                .FindIdentifierNode(property.ToString())?
                .ChildNodes()
                .OfType<Int32SignedLiteralValueSyntax>()
                .FirstOrDefault();

        if (intLiteral is null)
            return null;

        if (!int.TryParse(intLiteral.Number.ValueText, out int value))
            return null;

        return value;
    }

    public static bool? GetBooleanPropertyValue(this LabelPropertyValueSyntax? labelProperty, IdentifierProperty property) =>
        labelProperty?.Value.GetBooleanPropertyValue(property);

    public static bool? GetBooleanPropertyValue(this LabelSyntax? labelProperty, IdentifierProperty property) =>
        labelProperty?.Properties.GetBooleanPropertyValue(property);

    public static bool? GetBooleanPropertyValue(this SyntaxNode? node, IdentifierProperty property, bool includeChildNodes = false)
    {
        if (node is null)
            return null;

        var propertyList = node as CommaSeparatedIdentifierEqualsLiteralListSyntax;
        if (propertyList is not null)
            return GetBooleanPropertyValue(propertyList, property);

        if (!includeChildNodes)
            return null;

        var identifierEqualsLiteralList = FindIdentifierEqualsLiteralList(node);
        if (identifierEqualsLiteralList is null)
            return null;

        return GetBooleanPropertyValue(identifierEqualsLiteralList, property);
    }

    public static bool? GetBooleanPropertyValue(this CommaSeparatedIdentifierEqualsLiteralListSyntax? node, IdentifierProperty property)
    {
        if (node is null)
            return null;

        // Currently only 'Locked' property is supported
        if (property != IdentifierProperty.Locked)
            return null;

        var boolLiteral = node
                .FindIdentifierNode(property.ToString())?
                .ChildNodes()
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

    private static CommaSeparatedIdentifierEqualsLiteralListSyntax? FindIdentifierEqualsLiteralList(SyntaxNode node)
    {
        return node
            .ChildNodes()
            .OfType<CommaSeparatedIdentifierEqualsLiteralListSyntax>()
            .FirstOrDefault();
    }

    private static IdentifierEqualsLiteralSyntax? FindIdentifierNode(this CommaSeparatedIdentifierEqualsLiteralListSyntax list, string propertyName)
    {
        foreach (var entry in list.ChildNodes().OfType<IdentifierEqualsLiteralSyntax>())
        {
            if (string.Equals(entry.Identifier.ValueText, propertyName, StringComparison.OrdinalIgnoreCase))
                return entry;
        }

        return null;
    }
}