using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces.Formatting;
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
        // The spans can be present but empty for a document-scope Fix All: fix every diagnostic then.
        ImmutableArray<TextSpan> spans;
        if (fixAllSpans.HasValue && !fixAllSpans.Value.IsDefaultOrEmpty)
        {
            spans = fixAllSpans.Value;
        }
        else
        {
            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
            spans = diagnostics.Select(d => d.Location.SourceSpan).ToImmutableArray();
        }

        return await ApplyEditsAsync(document, spans, fixAllContext.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves every read against the original tree, then applies all edits in one bottom-up rewrite.
    /// Nested reads produce nested edits (an outer begin..end wrap contains an inner one), which
    /// independent per-diagnostic fixes cannot merge; single fixes take the same path for identical output.
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

        SyntaxTrivia documentLineBreak = FindLineBreak(root);

        // An insert rebuilds the statement list that owns the target; a wrap replaces the target itself,
        // and its if statement too when an else follows, so that the else can join the new end.
        var nodesToReplace = new HashSet<SyntaxNode>();
        foreach (StatementSyntax target in editsByTarget.Keys)
        {
            if (IsInStatementList(target))
            {
                nodesToReplace.Add(target.Parent);
                continue;
            }

            nodesToReplace.Add(target);
            if (JoinsElse(target))
                nodesToReplace.Add(target.Parent);
        }

        SyntaxNode newRoot = root.ReplaceNodes(nodesToReplace, (original, rewritten) =>
        {
            SyntaxNode result = InsertIntoStatementList(original, rewritten, editsByTarget);
            if (original is IfStatementSyntax ifStatement && result is IfStatementSyntax rewrittenIf
                && ifStatement.Statement is { } thenStatement
                && editsByTarget.ContainsKey(thenStatement) && JoinsElse(thenStatement))
            {
                result = JoinElseToEnd(rewrittenIf);
            }

            if (original is StatementSyntax statement && !IsInStatementList(statement)
                && editsByTarget.TryGetValue(statement, out var edits))
            {
                result = WrapInBlock(statement, (StatementSyntax)result, edits, documentLineBreak);
            }

            return result;
        });

        return document.WithSyntaxRoot(newRoot);
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

        StatementSyntax? target = FindTargetStatement(invocation);
        if (target is null)
            return null;

        // Wrapping rebuilds the statement and moves its trivia, which could unbalance or misplace
        // a compiler directive; offer no fix rather than risk changing what compiles. The only trivia
        // outside the statement that a wrap replaces is the whitespace before a following else, and
        // only when it is plain whitespace (JoinsElse), so a directive there stays in place.
        if (!IsInStatementList(target) && target.ContainsDirectives)
            return null;

        return new ReadTarget(invocation, memberAccess.Expression, variableSymbol, recordType, target);
    }

    private static ExpressionStatementSyntax BuildSetLoadFieldsStatement(
        ReadTarget read, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        // Collect accessed field names from the method body
        var fieldNames = CollectAccessedFields(
            read.Invocation, read.Variable, read.RecordType, semanticModel, cancellationToken);

        // Reuse the read's receiver expression, so the inserted call keeps the source spelling
        // (quotes and casing) instead of the symbol name. The elastic marker makes the CodeAction
        // formatter indent the inserted statement; source nodes lack the elastic trivia that
        // factory tokens carry.
        return BuildSetLoadFieldsStatement(
            read.Receiver.WithoutTrivia().WithLeadingTrivia(SyntaxFactory.ElasticMarker),
            fieldNames);
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

    private static ExpressionStatementSyntax BuildSetLoadFieldsStatement(
        CodeExpressionSyntax receiver, List<string> fieldNames)
    {
        var setLoadFieldsAccess = SyntaxFactory.MemberAccessExpression(
            receiver,
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName("SetLoadFields"));

        var arguments = new SeparatedSyntaxList<CodeExpressionSyntax>();
        foreach (var fieldName in fieldNames)
        {
            var fieldIdentifier = SyntaxFactory.IdentifierName(
                SyntaxFactory.Identifier(fieldName.QuoteIdentifierIfNeededWithReflection()));

            arguments = arguments.Add(fieldIdentifier);
        }

        var argumentList = SyntaxFactory.ArgumentList(arguments);
        var invocationExpr = SyntaxFactory.InvocationExpression(setLoadFieldsAccess, argumentList);

        return SyntaxFactory.ExpressionStatement(invocationExpr,
            SyntaxFactory.Token(EnumProvider.SyntaxKind.SemicolonToken));
    }

    /// <summary>
    /// The statement that receives the SetLoadFields call: the nearest statement around the read that
    /// either sits in a statement list (insert before it) or is the single statement embedded in an
    /// if/else branch, case line or loop body (wrap it in begin..end). A read inside asserterror climbs
    /// past it, so the call stays outside the statement that is expected to fail.
    /// </summary>
    private static StatementSyntax? FindTargetStatement(SyntaxNode node)
    {
        foreach (StatementSyntax statement in node.Ancestors().OfType<StatementSyntax>())
        {
            if (IsInStatementList(statement) || IsEmbeddedStatement(statement))
                return statement;
        }

        return null;
    }

    private static bool IsInStatementList(StatementSyntax statement) =>
        statement.Parent is BlockSyntax or RepeatStatementSyntax or CaseElseSyntax;

    private static bool IsEmbeddedStatement(StatementSyntax statement) =>
        statement.Parent is IfStatementSyntax or CaseLineSyntax or WhileStatementSyntax
            or ForStatementSyntax or ForEachStatementSyntax or WithStatementSyntax;

    private static SyntaxList<StatementSyntax> GetStatementList(SyntaxNode node) => node switch
    {
        BlockSyntax block => block.Statements,
        RepeatStatementSyntax repeat => repeat.Statements,
        CaseElseSyntax caseElse => caseElse.ElseStatements,
        _ => default,
    };

    private static SyntaxNode InsertIntoStatementList(SyntaxNode original, SyntaxNode rewritten,
        Dictionary<StatementSyntax, List<SetLoadFieldsEdit>> editsByTarget)
    {
        // Rewriting nested nodes replaces statements one-for-one, so the indexes still line up.
        SyntaxList<StatementSyntax> originalStatements = GetStatementList(original);
        SyntaxList<StatementSyntax> rewrittenStatements = GetStatementList(rewritten);
        if (originalStatements.Count == 0 || originalStatements.Count != rewrittenStatements.Count)
            return rewritten;

        var statements = new List<StatementSyntax>();
        for (int i = 0; i < originalStatements.Count; i++)
        {
            if (editsByTarget.TryGetValue(originalStatements[i], out var edits))
                statements.AddRange(edits.Select(edit => edit.Statement));

            statements.Add(rewrittenStatements[i]);
        }

        SyntaxList<StatementSyntax> newStatements = SyntaxFactory.List(statements);
        return rewritten switch
        {
            BlockSyntax block => block.WithStatements(newStatements),
            RepeatStatementSyntax repeat => repeat.WithStatements(newStatements),
            CaseElseSyntax caseElse => caseElse.WithElseStatements(newStatements),
            _ => rewritten,
        };
    }

    /// <summary>
    /// Whether the else after a wrapped then-branch moves onto the line of the new end keyword
    /// (end else). Only when nothing but whitespace separates them, so no comment or directive moves.
    /// </summary>
    private static bool JoinsElse(StatementSyntax statement) =>
        statement.Parent is IfStatementSyntax ifStatement
            && ifStatement.Statement == statement
            && ifStatement.ElseStatement is not null
            && IsWhitespace(statement.GetTrailingTrivia())
            && IsWhitespace(ifStatement.ElseKeywordToken.LeadingTrivia);

    private static bool IsWhitespace(SyntaxTriviaList triviaList) =>
        triviaList.All(trivia => trivia.ToString().All(char.IsWhiteSpace));

    /// <summary>
    /// Wraps <paramref name="statement"/> (the rewritten form of <paramref name="original"/>) in
    /// begin..end after the SetLoadFields calls. The block takes over the leading trivia and, when the
    /// statement ended with one, the semicolon that separates it from what follows.
    /// </summary>
    private static BlockSyntax WrapInBlock(StatementSyntax original, StatementSyntax statement,
        List<SetLoadFieldsEdit> edits, SyntaxTrivia documentLineBreak)
    {
        // The end keyword needs a real line break before it: an elastic one (the factory default)
        // makes the formatter leave a blank line after the wrapped statement. The trailing trivia ends
        // with that break unless a line comment ends it (the lexer then leaves the break to the next
        // token) or the next token shares the line. In those cases the trailing trivia moves behind
        // the block, so a comment ends up after the end keyword instead of swallowing it, and the end
        // keyword gets a line break of its own.
        SyntaxTriviaList trailingTrivia = statement.GetTrailingTrivia();
        SyntaxTrivia lineBreak = trailingTrivia.Count > 0 ? trailingTrivia[trailingTrivia.Count - 1] : default;
        bool statementEndsLine = lineBreak.Kind == EnumProvider.SyntaxKind.EndOfLineTrivia;
        if (!statementEndsLine)
            lineBreak = documentLineBreak;

        var innerStatement = statement.WithLeadingTrivia(SyntaxFactory.ElasticMarker);
        if (!statementEndsLine)
            innerStatement = innerStatement.WithoutTrailingTrivia();

        BlockSyntax block = SyntaxFactory.Block(SyntaxFactory.List(
            edits.Select(edit => (StatementSyntax)edit.Statement).Append(innerStatement)));

        block = block.WithEndKeywordToken(statementEndsLine
            ? block.EndKeywordToken.WithLeadingTrivia()
            : block.EndKeywordToken.WithLeadingTrivia(lineBreak));

        if (statement.GetLastToken().Kind == EnumProvider.SyntaxKind.SemicolonToken)
            block = block.WithSemicolonToken(SyntaxFactory.Token(EnumProvider.SyntaxKind.SemicolonToken));

        // A following else stays on the line of the end keyword (JoinElseToEnd).
        if (!statementEndsLine)
            block = block.WithTrailingTrivia(trailingTrivia);
        else if (!JoinsElse(original))
            block = block.WithTrailingTrivia(lineBreak);

        return block
            .WithLeadingTrivia(statement.GetLeadingTrivia())
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    /// <summary>The document's own line break, so the fix does not mix line endings.</summary>
    private static SyntaxTrivia FindLineBreak(SyntaxNode root)
    {
        SyntaxTrivia lineBreak = root.DescendantTrivia()
            .FirstOrDefault(trivia => trivia.Kind == EnumProvider.SyntaxKind.EndOfLineTrivia);
        return lineBreak.Kind == EnumProvider.SyntaxKind.EndOfLineTrivia
            ? lineBreak
            : SyntaxFactory.CarriageReturnLinefeed;
    }

    /// <summary>
    /// Replaces the whitespace before the else (see <see cref="JoinsElse"/>) with elastic trivia,
    /// so the formatter puts it on the line of the new end keyword.
    /// </summary>
    private static IfStatementSyntax JoinElseToEnd(IfStatementSyntax ifStatement) =>
        ifStatement.WithElseKeywordToken(ifStatement.ElseKeywordToken.WithLeadingTrivia(SyntaxFactory.ElasticMarker));

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
