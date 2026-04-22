using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(UsePartialRecordsOnReadCodeFixProvider))]
public sealed class UsePartialRecordsOnReadCodeFixProvider : CodeFixProvider
{
    private class UsePartialRecordsOnReadCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public UsePartialRecordsOnReadCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.UsePartialRecordsOnRead.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
         WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        Document document = ctx.Document;
        TextSpan span = ctx.Span;
        CancellationToken cancellationToken = ctx.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);

        SyntaxNode node = syntaxRoot.FindNode(span);
        if (node is not InvocationExpressionSyntax invocation)
            return;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        // Only offer fix for Record types (not RecordRef)
        SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken)
            .ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken);
        if (symbolInfo.Symbol is not IVariableSymbol variableSymbol)
            return;

        if (variableSymbol.Type is not IRecordTypeSymbol)
            return;

        ctx.RegisterCodeFix(
            CreateCodeAction(node, document, generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static UsePartialRecordsOnReadCodeAction CreateCodeAction(
        SyntaxNode node, Document document, bool generateFixAll)
    {
        return new UsePartialRecordsOnReadCodeAction(
            PlatformCopAnalyzers.UsePartialRecordsOnReadCodeAction,
            ct => InsertSetLoadFields(document, node, ct),
            nameof(UsePartialRecordsOnReadCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> InsertSetLoadFields(
        Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);
        Task<SemanticModel> semanticModelTask = document.GetSemanticModelAsync(cancellationToken);

        if (node is not InvocationExpressionSyntax invocation)
            return document;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return document;

        var semanticModel = await semanticModelTask.ConfigureAwait(false);
        if (semanticModel is null)
            return document;

        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken);
        if (symbolInfo.Symbol is not IVariableSymbol variableSymbol)
            return document;

        if (variableSymbol.Type is not IRecordTypeSymbol recordType)
            return document;

        var variableName = variableSymbol.Name;

        // Collect accessed field names from the method body
        var fieldNames = CollectAccessedFields(
            invocation, variableName, recordType, semanticModel, cancellationToken);

        // Build SetLoadFields invocation statement
        var setLoadFieldsStatement = BuildSetLoadFieldsStatement(variableName, fieldNames);

        // Find the statement containing the read operation
        var containingStatement = FindContainingStatement(invocation);
        if (containingStatement is null)
            return document;

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.InsertNodesBefore(containingStatement, new[] { setLoadFieldsStatement });
        return document.WithSyntaxRoot(newRoot);
    }

    private static List<string> CollectAccessedFields(
        InvocationExpressionSyntax readInvocation,
        string variableName,
        IRecordTypeSymbol recordType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Find the containing method/trigger body (not an inner begin..end block)
        var block = readInvocation.FirstAncestorOrSelf<MethodOrTriggerDeclarationSyntax>()?.Body;
        if (block is null)
            return GetPrimaryKeyFieldNames(recordType);

        var operation = semanticModel.GetOperation(block, cancellationToken);
        if (operation is null)
            return GetPrimaryKeyFieldNames(recordType);

        var collector = new FieldAccessCollector(variableName);
        collector.Visit(operation);

        if (collector.AccessedFields.Count == 0)
            return GetPrimaryKeyFieldNames(recordType);

        var fields = collector.AccessedFields.ToList();
        fields.Sort(StringComparer.OrdinalIgnoreCase);
        return fields;
    }

    private static List<string> GetPrimaryKeyFieldNames(IRecordTypeSymbol recordType)
    {
        var result = new List<string>();

        if (recordType.OriginalDefinition is ITableTypeSymbol tableType)
        {
            var pkFields = tableType.PrimaryKey?.Fields;
            if (pkFields is { IsDefault: false })
            {
                foreach (var field in pkFields)
                    result.Add(field.Name);
            }
        }

        return result;
    }

    private static ExpressionStatementSyntax BuildSetLoadFieldsStatement(
        string variableName, List<string> fieldNames)
    {
        var variableIdentifier = SyntaxFactory.IdentifierName(variableName);

        var setLoadFieldsAccess = SyntaxFactory.MemberAccessExpression(
            variableIdentifier,
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName("SetLoadFields"));

        var arguments = new SeparatedSyntaxList<CodeExpressionSyntax>();
        foreach (var fieldName in fieldNames)
        {
            var fieldAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxFactory.IdentifierName(variableName),
                SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
                SyntaxFactory.IdentifierName(
                    SyntaxFactory.Identifier(fieldName.QuoteIdentifierIfNeededWithReflection())));

            arguments = arguments.Add(fieldAccess);
        }

        var argumentList = SyntaxFactory.ArgumentList(arguments);
        var invocationExpr = SyntaxFactory.InvocationExpression(setLoadFieldsAccess, argumentList);

        return SyntaxFactory.ExpressionStatement(invocationExpr,
            SyntaxFactory.Token(EnumProvider.SyntaxKind.SemicolonToken));
    }

    private static StatementSyntax? FindContainingStatement(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is StatementSyntax statement && current.Parent is BlockSyntax)
                return statement;
            current = current.Parent;
        }
        return null;
    }

    private sealed class FieldAccessCollector : OperationWalker
    {
        /// <summary>
        /// Built-in Record methods where the first argument is a field selector (not a consumed value).
        /// Remaining arguments (if any) may contain consumed field accesses and are still visited.
        /// </summary>
        private static readonly HashSet<string> FirstArgFieldSelectorMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "SetRange", "SetFilter",
            "GetRangeMin", "GetRangeMax", "GetFilter",
            "SetAscending",
            "FieldCaption", "FieldName", "FieldNo",
            "HasFilter", "FieldActive"
        };

        /// <summary>
        /// Built-in Record methods where ALL arguments are field selectors (none are consumed values).
        /// </summary>
        private static readonly HashSet<string> AllArgsFieldSelectorMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "SetCurrentKey",
            "AddLoadFields", "LoadFields", "AreFieldsLoaded"
        };

        private readonly string _variableName;

        public HashSet<string> AccessedFields { get; } = new(StringComparer.OrdinalIgnoreCase);

        public FieldAccessCollector(string variableName)
        {
            _variableName = variableName;
        }

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            if (IsTrackedBuiltInMethodCall(operation, out var methodName))
            {
                if (AllArgsFieldSelectorMethods.Contains(methodName))
                    return;

                if (FirstArgFieldSelectorMethods.Contains(methodName)
                    && operation.Arguments.Length > 0)
                {
                    for (int i = 1; i < operation.Arguments.Length; i++)
                        Visit(operation.Arguments[i]);
                    return;
                }
            }

            base.VisitInvocationExpression(operation);
        }

        private bool IsTrackedBuiltInMethodCall(IInvocationExpression operation, out string methodName)
        {
            methodName = operation.TargetMethod.Name;

            if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
                return false;

            ISymbol? instanceSymbol = operation.Instance?.GetSymbol();
            return instanceSymbol != null
                && string.Equals(instanceSymbol.Name, _variableName, StringComparison.OrdinalIgnoreCase);
        }

        public override void VisitFieldAccess(IFieldAccess operation)
        {
            var fieldSymbol = operation.FieldSymbol;
            if (fieldSymbol is null)
                return;

            if (fieldSymbol.FieldClass != EnumProvider.FieldClassKind.Normal)
                return;

#if NETSTANDARD2_1
            var fieldType = fieldSymbol.OriginalDefinition.GetTypeSymbol();
            if (fieldType?.NavTypeKind == EnumProvider.NavTypeKind.Blob)
                return;
#else
            if (fieldSymbol.Type?.NavTypeKind == EnumProvider.NavTypeKind.Blob)
                return;
#endif

            ISymbol? instanceSymbol = operation.Instance?.GetSymbol();
            if (instanceSymbol is null ||
                !string.Equals(instanceSymbol.Name, _variableName, StringComparison.OrdinalIgnoreCase))
                return;

            AccessedFields.Add(fieldSymbol.Name);
        }
    }
}
