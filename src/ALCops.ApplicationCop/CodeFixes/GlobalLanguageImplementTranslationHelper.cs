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

[CodeFixProvider(nameof(GlobalLanguageImplementTranslationHelperCodeFixProvider))]
public sealed class GlobalLanguageImplementTranslationHelperCodeFixProvider : CodeFixProvider
{
    private const int DefaultGlobalLanguageId = 1033;
    private const string TranslationHelperCodeunitName = "Translation Helper";
    private const string DefaultVariableName = "TranslationHelper";
    private const string SetGlobalLanguageByIdMethodName = "SetGlobalLanguageById";
    private const string SetGlobalLanguageToDefaultMethodName = "SetGlobalLanguageToDefault";

    private class GlobalLanguageImplementTranslationHelperCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.Refactor;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public GlobalLanguageImplementTranslationHelperCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.GlobalLanguageImplementTranslationHelper.Id);

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
        ctx.RegisterCodeFix(CreateCodeAction(node, document, false), ctx.Diagnostics[0]);
    }

    private static GlobalLanguageImplementTranslationHelperCodeAction CreateCodeAction(SyntaxNode node, Document document,
        bool generateFixAll)
    {
        return new GlobalLanguageImplementTranslationHelperCodeAction(
            ApplicationCopAnalyzers.GlobalLanguageImplementTranslationHelperCodeAction,
            ct => ImplementTranslationHelperCodeAction(document, node, ct),
            nameof(GlobalLanguageImplementTranslationHelperCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ImplementTranslationHelperCodeAction(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var originalInvocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>(); // GlobalLanguage(1033);
        var originalAssignment = node.FirstAncestorOrSelf<AssignmentStatementSyntax>();  // GlobalLanguage := 1033;
        if (originalInvocation is null && originalAssignment is null)
            return document;

        // For assignment we want to replace the whole statement; for invocation we replace the invocation expression.
        SyntaxNode anchorNode = (SyntaxNode?)originalAssignment ?? originalInvocation!;

        var containingMethodOrTrigger = GetContainingMethodOrTrigger(anchorNode);
        if (containingMethodOrTrigger is null)
            return document;

        var containingObject = GetContainingApplicationObject(containingMethodOrTrigger);
        if (containingObject is null)
            return document;

        var existingVariableName = FindExistingTranslationHelperVariable(containingMethodOrTrigger, containingObject);
        var variableName = existingVariableName ?? DefaultVariableName;

        // Track nodes across edits so we always operate on nodes from the current tree
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var trackedRoot = root.TrackNodes(containingMethodOrTrigger, anchorNode);

        var currentMethodOrTrigger = trackedRoot.GetCurrentNode(containingMethodOrTrigger);
        var currentAnchorNode = trackedRoot.GetCurrentNode(anchorNode);
        if (currentMethodOrTrigger is null || currentAnchorNode is null)
            return document;

        SyntaxNode newRoot;
        switch (currentAnchorNode)
        {
            // GlobalLanguage(1033);
            case InvocationExpressionSyntax currentInvocation:
                {
                    var firstArgExpr =
                        currentInvocation.ArgumentList.Arguments.Count > 0
                            ? currentInvocation.ArgumentList.Arguments[0]
                            : null;

                    var replacementInvocation =
                        CreateSetGlobalLanguageInvocation(
                                variableName,
                                currentInvocation.ArgumentList,
                                firstArgExpr)
                            .WithLeadingTrivia(currentInvocation.GetLeadingTrivia())
                            .WithTrailingTrivia(currentInvocation.GetTrailingTrivia());

                    newRoot = trackedRoot.ReplaceNode(currentInvocation, replacementInvocation);
                    break;
                }

            // GlobalLanguage := 1033;
            case AssignmentStatementSyntax currentAssignment:
                {
                    var args = default(SeparatedSyntaxList<CodeExpressionSyntax>).Add(currentAssignment.Source);
                    var argumentList = SyntaxFactory.ArgumentList(args);

                    var invocation =
                        CreateSetGlobalLanguageInvocation(
                            variableName,
                            argumentList,
                            currentAssignment.Source);

                    var replacementStatement =
                        SyntaxFactory.ExpressionStatement(invocation, currentAssignment.SemicolonToken)
                            .WithLeadingTrivia(currentAssignment.GetLeadingTrivia())
                            .WithTrailingTrivia(currentAssignment.GetTrailingTrivia());

                    newRoot = trackedRoot.ReplaceNode(currentAssignment, replacementStatement);
                    break;
                }

            default:
                return document;
        }

        // If needed add "Translation Helper" codeunit as a local variable
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

    #region Variable Helpers

    private static string? FindExistingTranslationHelperVariable(MethodOrTriggerDeclarationSyntax methodOrTrigger, ApplicationObjectSyntax containingObject)
    {
        var localVarName = FindTranslationHelperVariableInVarSection(methodOrTrigger.Variables);
        if (localVarName is not null)
            return localVarName;

        var globalVarName = FindTranslationHelperVariableInMembers(containingObject.Members);
        return globalVarName;
    }

    private static string? FindTranslationHelperVariableInVarSection(VarSectionBaseSyntax? varSection)
    {
        if (varSection is null)
            return null;

        foreach (var variable in varSection.Variables)
        {
            if (IsTranslationHelperCodeunitVariable(variable))
            {
                return variable.GetIdentifierNameSyntax().Identifier.ValueText?.UnquoteIdentifier();
            }
        }
        return null;
    }

    private static string? FindTranslationHelperVariableInMembers(SyntaxList<MemberSyntax> members)
    {
        foreach (var member in members)
        {
            if (member is GlobalVarSectionSyntax globalVarSection)
            {
                foreach (var variable in globalVarSection.Variables)
                {
                    if (IsTranslationHelperCodeunitVariable(variable))
                    {
                        return variable.GetIdentifierNameSyntax().Identifier.ValueText?.UnquoteIdentifier();
                    }
                }
            }
        }
        return null;
    }

    private static bool IsTranslationHelperCodeunitVariable(VariableDeclarationBaseSyntax variable)
    {
        if (variable.Type is not TypeReferenceBaseSyntax typeReference)
            return false;

        if (typeReference.DataType.TypeName.Kind != EnumProvider.SyntaxKind.CodeunitKeyword)
            return false;

        // Check if matches "Translation Helper"
        return string.Equals(GetSubtypeName(typeReference.DataType), TranslationHelperCodeunitName, StringComparison.OrdinalIgnoreCase);
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
        var variableDeclaration = CreateTranslationHelperVariableDeclaration(variableName);

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

    private static InvocationExpressionSyntax CreateSetGlobalLanguageInvocation(string variableName, ArgumentListSyntax args, CodeExpressionSyntax? singleValueExpressionFor1033Check = null)
    {
        if (singleValueExpressionFor1033Check is not null &&
            IsLiteralIntValue(singleValueExpressionFor1033Check, DefaultGlobalLanguageId))
        {
            return CreateSetGlobalLanguageToDefaultInvocation(variableName);
        }

        return CreateSetGlobalLanguageByIdInvocation(variableName, args);
    }

    private static bool IsLiteralIntValue(CodeExpressionSyntax codeExpression, int expected)
    {
        if (codeExpression is not LiteralExpressionSyntax literalExpression)
            return false;

        if (literalExpression.Literal is not Int32SignedLiteralValueSyntax syntax)
            return false;

        return int.TryParse(syntax.Number.ValueText, out var value) &&
               value == expected;
    }

    private static InvocationExpressionSyntax CreateSetGlobalLanguageToDefaultInvocation(string variableName)
    {
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxFactory.IdentifierName(variableName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName(SetGlobalLanguageToDefaultMethodName));

        return SyntaxFactory.InvocationExpression(memberAccess, SyntaxFactory.ArgumentList());
    }

    private static InvocationExpressionSyntax CreateSetGlobalLanguageByIdInvocation(string variableName, ArgumentListSyntax originalArguments)
    {
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxFactory.IdentifierName(variableName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName(SetGlobalLanguageByIdMethodName));

        return SyntaxFactory.InvocationExpression(memberAccess, originalArguments);
    }

    private static VariableDeclarationSyntax CreateTranslationHelperVariableDeclaration(string variableName)
    {
        return SyntaxFactory.VariableDeclaration(
            default, // empty SyntaxList<MemberAttributeSyntax>
            SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(variableName)),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.ColonToken),
            CreateCodeunitTypeReference(TranslationHelperCodeunitName),
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