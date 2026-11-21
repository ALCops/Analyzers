using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Helpers;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Editing;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
#if NETSTANDARD2_1
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
#endif

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(UsePartialRecordsOnReadCodeFixProvider))]
public sealed class UsePartialRecordsOnReadCodeFixProvider : CodeFixProvider
{
    private sealed class UsePartialRecordsOnReadCodeAction : CodeAction.DocumentChangeAction
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
        FixAllProvider.Create(FixAllAsync);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        Document document = context.Document;
        TextSpan span = context.Span;
        CancellationToken cancellationToken = context.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);

        SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken)
            .ConfigureAwait(false);
        if (semanticModel is null)
            return;

        // Applicability only; the field list is collected when the action runs.
        if (TryResolveRead(syntaxRoot.FindNode(span, getInnermostNodeForTie: true), semanticModel, cancellationToken) is null)
            return;

        context.RegisterCodeFix(
            CreateCodeAction(span, document, generateFixAll: true),
            context.Diagnostics[0]);
    }

    private static UsePartialRecordsOnReadCodeAction CreateCodeAction(
        TextSpan span, Document document, bool generateFixAll)
    {
        return new UsePartialRecordsOnReadCodeAction(
            PlatformCopAnalyzers.UsePartialRecordsOnReadCodeAction,
            ct => ApplyEditsAsync(document, ImmutableArray.Create(span), ct),
            nameof(UsePartialRecordsOnReadCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document,
        Optional<ImmutableArray<TextSpan>> fixAllSpans)
    {
        // The SDK passes empty spans for every Fix All scope. Non-empty spans would restrict the fix
        // to a region rather than name the diagnostics, so they only filter the document's diagnostics.
        var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
        ImmutableArray<TextSpan> regions = fixAllSpans.HasValue ? fixAllSpans.Value : default;
        ImmutableArray<TextSpan> spans = diagnostics
            .Select(diagnostic => diagnostic.Location.SourceSpan)
            .Where(span => regions.IsDefaultOrEmpty || regions.Any(region => region.IntersectsWith(span)))
            .ToImmutableArray();

        // Returns the unchanged document when every read bails out, as the house Fix All template does.
        return await ApplyEditsAsync(document, spans, fixAllContext.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves every read against the original tree, then applies all edits through one
    /// <see cref="SyntaxEditor"/>, which composes nested edits (an outer begin..end wrap around an inner
    /// one) that independent per-diagnostic fixes cannot merge; single fixes take the same path.
    /// </summary>
    private static async Task<Document> ApplyEditsAsync(
        Document document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
    {
        if (spans.IsDefaultOrEmpty)
            return document;

        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || semanticModel is null)
            return document;

        // One SetLoadFields per variable per target statement, in source order of the reads.
        var editsByTarget = new Dictionary<StatementSyntax, List<SetLoadFieldsEdit>>();
        foreach (TextSpan span in spans.OrderBy(span => span.Start))
        {
            ReadTarget? read = TryResolveRead(root.FindNode(span, getInnermostNodeForTie: true), semanticModel, cancellationToken);
            if (read is null)
                continue;

            if (!editsByTarget.TryGetValue(read.Target, out var edits))
            {
                edits = new List<SetLoadFieldsEdit>();
                editsByTarget.Add(read.Target, edits);
            }

            if (!edits.Any(existing => existing.Variable.Equals(read.Variable)))
                edits.Add(new SetLoadFieldsEdit(read.Variable, BuildSetLoadFieldsStatement(read, semanticModel, cancellationToken)));
        }

        if (editsByTarget.Count == 0)
            return document;

        var editor = new SyntaxEditor(root);
        SyntaxTrivia lineBreak = root.GetLineBreak();
        // Source order of the targets, so the edit sequence does not depend on dictionary enumeration.
        foreach (var (target, edits) in editsByTarget.OrderBy(pair => pair.Key.SpanStart))
            editor.InsertStatementsBefore(target, edits.Select(edit => edit.Statement).ToList(), lineBreak);

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }

    /// <summary>
    /// The read at <paramref name="node"/> and the statement that receives its SetLoadFields call,
    /// or null when no fix applies. Cheap enough for <see cref="RegisterCodeFixesAsync"/>.
    /// </summary>
    private static ReadTarget? TryResolveRead(
        SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (node is not InvocationExpressionSyntax invocation)
            return null;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return null;

        // Only offer fix for Record types (not RecordRef)
        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken);
        if (symbolInfo.Symbol is not IVariableSymbol variableSymbol)
            return null;

        if (variableSymbol.Type is not IRecordTypeSymbol recordType)
            return null;

        // asserterror is not an embedding parent: a single-statement body is never wrapped and the
        // climb continues to the asserterror statement; a block body takes the insert inside it.
        StatementSyntax? target = invocation.FindStatementInsertionTarget();
        if (target is null || target.BlocksStatementInsertion())
            return null;

        return new ReadTarget(invocation, memberAccess.Expression, variableSymbol, recordType, target);
    }

    private static ExpressionStatementSyntax BuildSetLoadFieldsStatement(
        ReadTarget read, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var fieldNames = CollectAccessedFields(
            read.Invocation, read.Variable, read.RecordType, semanticModel, cancellationToken);

        // Reuse the read's receiver expression, so the inserted call keeps the source spelling
        // (quotes and casing) instead of the symbol name. The elastic marker makes the CodeAction
        // formatter indent the inserted statement; source nodes lack the elastic trivia that
        // factory tokens carry.
        return SyntaxFactoryHelper.MemberInvocationStatement(
            read.Receiver.WithoutTrivia().WithLeadingTrivia(SyntaxFactory.ElasticMarker),
            "SetLoadFields",
            fieldNames.Select(fieldName => (CodeExpressionSyntax)SyntaxFactory.IdentifierName(
                SyntaxFactory.Identifier(fieldName.QuoteIdentifierIfNeededWithReflection()))));
    }

    private static List<string> CollectAccessedFields(
        InvocationExpressionSyntax readInvocation,
        IVariableSymbol variable,
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

        var collector = new FieldAccessCollector(variable);
        collector.Visit(operation);

        if (collector.AccessedFields.Count == 0)
            return GetPrimaryKeyFieldNames(recordType);

        var fields = collector.AccessedFields.ToList();
        fields.Sort(SemanticFacts.NameComparer);
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

    private sealed class ReadTarget
    {
        public InvocationExpressionSyntax Invocation { get; }
        public CodeExpressionSyntax Receiver { get; }
        public IVariableSymbol Variable { get; }
        public IRecordTypeSymbol RecordType { get; }
        public StatementSyntax Target { get; }

        public ReadTarget(InvocationExpressionSyntax invocation, CodeExpressionSyntax receiver,
            IVariableSymbol variable, IRecordTypeSymbol recordType, StatementSyntax target)
        {
            Invocation = invocation;
            Receiver = receiver;
            Variable = variable;
            RecordType = recordType;
            Target = target;
        }
    }

    private sealed class SetLoadFieldsEdit
    {
        public IVariableSymbol Variable { get; }
        public ExpressionStatementSyntax Statement { get; }

        public SetLoadFieldsEdit(IVariableSymbol variable, ExpressionStatementSyntax statement)
        {
            Variable = variable;
            Statement = statement;
        }
    }

    private sealed class FieldAccessCollector : OperationWalker
    {
        /// <summary>
        /// Built-in Record methods where the first argument is a field selector (not a consumed value).
        /// Remaining arguments (if any) may contain consumed field accesses and are still visited.
        /// </summary>
        private static readonly HashSet<string> FirstArgFieldSelectorMethods = new(SemanticFacts.NameEqualityComparer)
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
        private static readonly HashSet<string> AllArgsFieldSelectorMethods = new(SemanticFacts.NameEqualityComparer)
        {
            "SetCurrentKey",
            "AddLoadFields", "LoadFields", "AreFieldsLoaded"
        };

        private readonly IVariableSymbol _variable;

        public HashSet<string> AccessedFields { get; } = new(SemanticFacts.NameEqualityComparer);

        public FieldAccessCollector(IVariableSymbol variable)
        {
            _variable = variable;
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

            // Symbol identity: `this.<Global>` can share its name with the diagnosed local.
            ISymbol? instanceSymbol = operation.Instance?.GetSymbolSafe();
            return instanceSymbol != null && instanceSymbol.Equals(_variable);
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

            ISymbol? instanceSymbol = operation.Instance?.GetSymbolSafe();
            if (instanceSymbol is null || !instanceSymbol.Equals(_variable))
                return;

            AccessedFields.Add(fieldSymbol.Name);
        }
    }
}
