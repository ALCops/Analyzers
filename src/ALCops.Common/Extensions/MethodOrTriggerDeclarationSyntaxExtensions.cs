using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.Common.Extensions;

public static class MethodOrTriggerDeclarationSyntaxExtensions
{
    /// <summary>
    /// Returns the unquoted name of a <c>Codeunit "codeunitName"</c> variable visible in the method or trigger:
    /// its local var section first, then the global var sections of the containing object. Null when none exists.
    /// </summary>
    public static string? FindCodeunitVariableName(this MethodOrTriggerDeclarationSyntax methodOrTrigger, string codeunitName)
    {
        var localName = FindCodeunitVariableName(methodOrTrigger.Variables?.Variables, codeunitName);
        if (localName is not null)
            return localName;

        var containingObject = methodOrTrigger.FirstAncestorOrSelf<ApplicationObjectSyntax>();
        if (containingObject is null)
            return null;

        foreach (var member in containingObject.Members)
        {
            if (member is GlobalVarSectionSyntax globalVarSection &&
                FindCodeunitVariableName(globalVarSection.Variables, codeunitName) is { } globalName)
            {
                return globalName;
            }
        }

        return null;
    }

    /// <summary>
    /// Appends <paramref name="variable"/> to the method's or trigger's local var section, creating the section when absent.
    /// Returns the updated declaration; the caller replaces the original node in its tree.
    /// </summary>
    public static MethodOrTriggerDeclarationSyntax AddLocalVariable(this MethodOrTriggerDeclarationSyntax methodOrTrigger, VariableDeclarationBaseSyntax variable)
    {
        if (methodOrTrigger.Variables is VarSectionSyntax existingVarSection)
            return methodOrTrigger.ReplaceNode(existingVarSection, existingVarSection.WithVariables(existingVarSection.Variables.Add(variable)));

        var newVarSection = SyntaxFactory.VarSection(
            SyntaxFactory.Token(EnumProvider.SyntaxKind.VarKeyword),
            new SyntaxList<VariableDeclarationBaseSyntax>().Add(variable));

        // MethodOrTriggerDeclarationSyntax has no WithVariables; only the concrete types do.
        return methodOrTrigger switch
        {
            MethodDeclarationSyntax method => method.WithVariables(newVarSection),
            TriggerDeclarationSyntax trigger => trigger.WithVariables(newVarSection),
            _ => methodOrTrigger
        };
    }

    private static string? FindCodeunitVariableName(SyntaxList<VariableDeclarationBaseSyntax>? variables, string codeunitName)
    {
        if (variables is null)
            return null;

        foreach (var variable in variables.Value)
        {
            if (variable.IsCodeunitVariable(codeunitName))
                return variable.GetIdentifierNameSyntax().Identifier.ValueText?.UnquoteIdentifier();
        }

        return null;
    }
}
