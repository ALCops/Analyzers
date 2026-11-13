using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Helpers;

/// <summary>
/// Builds <c>Name: Codeunit "Codeunit Name";</c> variable declarations for CodeFixes.
/// </summary>
public static class CodeunitVariableFactory
{
    public static VariableDeclarationSyntax CreateDeclaration(string variableName, string codeunitName)
    {
        return SyntaxFactory.VariableDeclaration(
            default, // empty SyntaxList<MemberAttributeSyntax>
            SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(variableName)),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.ColonToken),
            CreateTypeReference(codeunitName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.SemicolonToken));
    }

    public static SimpleTypeReferenceSyntax CreateTypeReference(string codeunitName)
    {
        var codeunitObjectNameOrId =
            SyntaxFactory.ObjectNameOrId(
                SyntaxFactory.IdentifierName(
                    SyntaxFactory.Identifier(codeunitName)));

        var codeunitDataType =
            SyntaxFactory.SubtypedDataType(
                SyntaxFactory.ParseKeyword("Codeunit"),
                codeunitObjectNameOrId);

        return SyntaxFactory.SimpleTypeReference(codeunitDataType);
    }
}
