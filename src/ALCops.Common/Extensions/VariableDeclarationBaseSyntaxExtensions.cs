using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.Common.Extensions;

public static class VariableDeclarationBaseSyntaxExtensions
{
    /// <summary>
    /// True when the variable is declared as <c>Codeunit "codeunitName"</c> (AL name comparison, quotes ignored).
    /// </summary>
    public static bool IsCodeunitVariable(this VariableDeclarationBaseSyntax variable, string codeunitName)
    {
        if (variable.Type is not TypeReferenceBaseSyntax typeReference)
            return false;

        if (typeReference.DataType.TypeName.Kind != EnumProvider.SyntaxKind.CodeunitKeyword)
            return false;

        return GetSubtypeName(typeReference.DataType).IsSameName(codeunitName);
    }

    private static string? GetSubtypeName(DataTypeSyntax dataType)
    {
        if (dataType is not SubtypedDataTypeSyntax dataTypeWithSubtype)
            return null;

        if (dataTypeWithSubtype.Subtype is ObjectNameOrIdSyntax { Identifier: IdentifierNameSyntax identifierName })
            return identifierName.Identifier.ValueText?.UnquoteIdentifier();

        return dataTypeWithSubtype.Subtype.Identifier?.ToString().UnquoteIdentifier();
    }
}
