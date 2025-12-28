using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(RunPageImplementPageManagementCodeFixProvider))]
public sealed class RunPageImplementPageManagementCodeFixProvider : CodeFixProvider
{
    private const string PageManagementCodeunitName = "Page Management";
    private const string DefaultVariableName = "PageManagement";
    private const string PageRunMethodName = "PageRun";
    private const string PageRunModalMethodName = "PageRunModal";
    private const string PageRunAtFieldMethodName = "PageRunAtField";

    private class RunPageImplementPageManagementCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.Refactor;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public RunPageImplementPageManagementCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.RunPageImplementPageManagement.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
         WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        Document document = ctx.Document;
        TextSpan span = ctx.Span;
        CancellationToken cancellationToken = ctx.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        RegisterInstanceCodeFix(ctx, syntaxRoot, span, document);
    }

    private static void RegisterInstanceCodeFix(CodeFixContext ctx, SyntaxNode syntaxRoot, TextSpan span, Document document)
    {
        SyntaxNode node = syntaxRoot.FindNode(span);
        ctx.RegisterCodeFix(CreateCodeAction(node, document, generateFixAll: false), ctx.Diagnostics[0]);
    }

    private static RunPageImplementPageManagementCodeAction CreateCodeAction(SyntaxNode node, Document document,
        bool generateFixAll)
    {
        return new RunPageImplementPageManagementCodeAction(
            ApplicationCopAnalyzers.RunPageImplementPageManagementCodeAction,
            ct => ImplementPageManagement(document, node, ct),
            nameof(RunPageImplementPageManagementCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ImplementPageManagement(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var originalInvocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>(); // Page.Run(...);
        if (originalInvocation is null)
            return document;

        var containingMethodOrTrigger = GetContainingMethodOrTrigger(originalInvocation);
        if (containingMethodOrTrigger is null)
            return document;

        var containingObject = GetContainingApplicationObject(containingMethodOrTrigger);
        if (containingObject is null)
            return document;

        var existingVariableName = FindExistingPageManagementVariable(containingMethodOrTrigger, containingObject);
        var variableName = existingVariableName ?? DefaultVariableName;

        // Define the corresonding method name based on original invocation
        if (originalInvocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return document;

        var runModel = string.Equals(originalInvocation.Expression.GetNameStringValue(), "RunModal", StringComparison.OrdinalIgnoreCase);
        var methodName = GetMethodNameForPageManagement(originalInvocation, runModel);

        // Track nodes across edits so we always operate on nodes from the current tree
        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var trackedRoot = root.TrackNodes(containingMethodOrTrigger, originalInvocation);

        var currentMethodOrTrigger = trackedRoot.GetCurrentNode(containingMethodOrTrigger);
        var currentoriginalInvocation = trackedRoot.GetCurrentNode(originalInvocation);
        if (currentMethodOrTrigger is null || currentoriginalInvocation is null)
            return document;

        SyntaxNode newRoot;
        switch (currentoriginalInvocation)
        {
            case InvocationExpressionSyntax currentInvocation:
                {
                    var replacementInvocation =
                        CreateRunWithPageManagementCodeUnit(
                                methodName,
                                variableName,
                                runModel,
                                currentInvocation.ArgumentList)
                            .WithLeadingTrivia(currentInvocation.GetLeadingTrivia())
                            .WithTrailingTrivia(currentInvocation.GetTrailingTrivia());

                    newRoot = trackedRoot.ReplaceNode(currentInvocation, replacementInvocation);
                    break;
                }

            default:
                return document;
        }

        // If needed add "Page Management" codeunit as a local variable
        if (existingVariableName is null)
        {
            var updatedMethodOrTrigger = newRoot.GetCurrentNode(containingMethodOrTrigger);
            if (updatedMethodOrTrigger is not null)
                newRoot = AddLocalVariable(newRoot, updatedMethodOrTrigger, variableName);
        }

        return document.WithSyntaxRoot(newRoot);
    }

    #region Object Helpers

    private static MethodOrTriggerDeclarationSyntax? GetContainingMethodOrTrigger(SyntaxNode node)
    {
        var current = node.Parent;
        while (current is not null)
        {
            if (current is MethodOrTriggerDeclarationSyntax methodOrTrigger)
                return methodOrTrigger;
            current = current.Parent;
        }
        return null;
    }

    private static ApplicationObjectSyntax? GetContainingApplicationObject(SyntaxNode node)
    {
        var current = node.Parent;
        while (current is not null)
        {
            if (current is ApplicationObjectSyntax applicationObject)
                return applicationObject;
            current = current.Parent;
        }
        return null;
    }

    #endregion

    #region Method Helpers
    private static string GetMethodNameForPageManagement(InvocationExpressionSyntax invocationExpressionSyntax, bool runModel)
    {
        if (invocationExpressionSyntax.ArgumentList.Arguments.Count == 3 &&
            IsLiteralIntValue(invocationExpressionSyntax.ArgumentList.Arguments[2]))
        {
            return PageRunAtFieldMethodName;
        }

        if (runModel)
        {
            return PageRunModalMethodName;
        }

        return PageRunMethodName;
    }

    #endregion


    #region Variable Helpers

    private static string? FindExistingPageManagementVariable(MethodOrTriggerDeclarationSyntax methodOrTrigger, ApplicationObjectSyntax containingObject)
    {
        var localVarName = FindPageManagementVariableInVarSection(methodOrTrigger.Variables);
        if (localVarName is not null)
            return localVarName;

        var globalVarName = FindPageManagementVariableInMembers(containingObject.Members);
        return globalVarName;
    }

    private static string? FindPageManagementVariableInVarSection(VarSectionBaseSyntax? varSection)
    {
        if (varSection is null)
            return null;

        foreach (var variable in varSection.Variables)
        {
            if (IsPageManagementCodeunitVariable(variable))
            {
                return variable.GetIdentifierNameSyntax().Identifier.ValueText?.UnquoteIdentifier();
            }
        }
        return null;
    }

    private static string? FindPageManagementVariableInMembers(SyntaxList<MemberSyntax> members)
    {
        foreach (var member in members)
        {
            if (member is GlobalVarSectionSyntax globalVarSection)
            {
                foreach (var variable in globalVarSection.Variables)
                {
                    if (IsPageManagementCodeunitVariable(variable))
                    {
                        return variable.GetIdentifierNameSyntax().Identifier.ValueText?.UnquoteIdentifier();
                    }
                }
            }
        }
        return null;
    }

    private static bool IsPageManagementCodeunitVariable(VariableDeclarationBaseSyntax variable)
    {
        if (variable.Type is not TypeReferenceBaseSyntax typeReference)
            return false;

        if (typeReference.DataType.TypeName.Kind != EnumProvider.SyntaxKind.CodeunitKeyword)
            return false;

        // Check if matches "Page Management"
        return string.Equals(GetSubtypeName(typeReference.DataType), PageManagementCodeunitName, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetSubtypeName(DataTypeSyntax dataType)
    {
        if (dataType is not SubtypedDataTypeSyntax dataTypeWithSubtype)
            return null;

        if (dataTypeWithSubtype.Subtype is ObjectNameOrIdSyntax objectNameOrId)
        {
            if (objectNameOrId.Identifier is IdentifierNameSyntax IdentifierName)
            {
                return IdentifierName.Identifier.ValueText?.UnquoteIdentifier();
            }
        }

        return dataTypeWithSubtype.Subtype.Identifier?.ToString().UnquoteIdentifier();
    }

    private static SyntaxNode AddLocalVariable(SyntaxNode root, MethodOrTriggerDeclarationSyntax methodOrTrigger, string variableName)
    {
        var variableDeclaration = CreatePageManagementVariableDeclaration(variableName);

        if (methodOrTrigger.Variables is VarSectionSyntax existingVarSection)
        {
            var newVariables = existingVarSection.Variables.Add(variableDeclaration);
            var newVarSection2 = existingVarSection.WithVariables(newVariables);
            return root.ReplaceNode(existingVarSection, newVarSection2);
        }

        var newVarSection = SyntaxFactory.VarSection(
            SyntaxFactory.Token(EnumProvider.SyntaxKind.VarKeyword),
            new SyntaxList<VariableDeclarationBaseSyntax>().Add(variableDeclaration));

        // The generic MethodOrTriggerDeclarationSyntax class doesn't have WithVariables method, so we need the concrete types
        var newMethodOrTrigger =
            methodOrTrigger switch
            {
                MethodDeclarationSyntax method => method.WithVariables(newVarSection),
                TriggerDeclarationSyntax trigger => trigger.WithVariables(newVarSection),
                _ => methodOrTrigger
            };

        return root.ReplaceNode(methodOrTrigger, newMethodOrTrigger);
    }

    #endregion

    #region Invocation Helpers

    private static bool IsLiteralIntValue(CodeExpressionSyntax codeExpression)
    {
        if (codeExpression is not LiteralExpressionSyntax literalExpression)
            return false;

        if (literalExpression.Literal is not Int32SignedLiteralValueSyntax syntax)
            return false;

        return int.TryParse(syntax.Number.ValueText, out var value);
    }

    private static InvocationExpressionSyntax CreateRunWithPageManagementCodeUnit(string methodName, string variableName, bool runModel, ArgumentListSyntax originalArguments)
    {
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxFactory.IdentifierName(variableName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName(methodName));

        var newArguments = CreateNewArguments(originalArguments, methodName, runModel);

        return SyntaxFactory.InvocationExpression(memberAccess, newArguments);
    }

    private static ArgumentListSyntax CreateNewArguments(ArgumentListSyntax originalArguments, string methodName, bool runModel)
    {
        var identifier = originalArguments.Arguments
            .OfType<IdentifierNameSyntax>()
            .First();

        if (string.Equals(methodName, PageRunAtFieldMethodName, StringComparison.Ordinal))
        {
            var fieldExpr = originalArguments.Arguments
                .OfType<LiteralExpressionSyntax>()
                .Last();

            var boolExpr =
                SyntaxFactory.LiteralExpression(
                    SyntaxFactory.BooleanLiteralValue(
                        SyntaxFactory.Token(runModel
                            ? EnumProvider.SyntaxKind.TrueKeyword
                            : EnumProvider.SyntaxKind.FalseKeyword)));

            return SyntaxFactory.ArgumentList(
                new SeparatedSyntaxList<CodeExpressionSyntax>()
                    .Add(identifier)
                    .Add(fieldExpr)
                    .Add(boolExpr));
        }

        return SyntaxFactory.ArgumentList(
            new SeparatedSyntaxList<CodeExpressionSyntax>()
                .Add(identifier));
    }

    private static VariableDeclarationSyntax CreatePageManagementVariableDeclaration(string variableName)
    {
        return SyntaxFactory.VariableDeclaration(
            default, // empty SyntaxList<MemberAttributeSyntax>
            SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(variableName)),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.ColonToken),
            CreateCodeunitTypeReference(PageManagementCodeunitName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.SemicolonToken));
    }

    private static SimpleTypeReferenceSyntax CreateCodeunitTypeReference(string codeunitName)
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

    #endregion
}