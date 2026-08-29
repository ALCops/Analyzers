using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

using TableTransfer = (Microsoft.Dynamics.Nav.CodeAnalysis.ITableTypeSymbol Source,
    Microsoft.Dynamics.Nav.CodeAnalysis.ITableTypeSymbol Destination);

namespace ALCops.Common.Permissions;

/// <summary>
/// Resolves which tables a <c>DataTransfer</c> executor (<c>CopyFields</c>/<c>CopyRows</c>)
/// actually transfers, by walking the enclosing method or trigger body in flow order and
/// pairing every executor with the <c>SetTables(Database::X, Database::Y)</c> calls that
/// reach it.
/// <para>
/// State is kept per <c>DataTransfer</c> variable (by the name the receiver resolves to).
/// A <c>SetTables</c> <em>replaces</em> the pending pair for that variable on the current path,
/// because AL code commonly reconfigures one transfer variable for several sequential copies.
/// An executor consumes the pending pairs without clearing them, so
/// <c>SetTables; CopyRows; CopyFields</c> attributes both executors to the same pair.
/// Branches fork and merge like PC0030's <c>SetLoadFieldsWalker</c>; a merge is the
/// <em>union</em> of the branches' pending pairs, and <c>break</c> contributes the state at
/// the jump to the states after the loop.
/// </para>
/// <para>
/// Loop bodies are visited twice: a <c>SetTables</c> written after an executor inside the body
/// reaches that executor through the loop's back edge, and the second pass (started from the
/// merge of the first) lets the executor see it. Per variable the transfer function is
/// "constant, or pass the incoming state through", so the union lattice converges in that one
/// extra pass for a single loop; deeply nested loops could in principle need more.
/// </para>
/// <para>
/// One accepted imprecision remains: <c>exit</c> does not terminate a path, so state from
/// before an early exit still flows forward and an executor can be attributed a table the
/// path never reaches. That over-attribution is conservative for AC0032 (a permission can only
/// look used, never unused), but it can make AC0031 ask for a permission the code does not
/// need.
/// </para>
/// </summary>
public sealed class DataTransferTableResolver
{
    /// <summary>
    /// The single encoding of "unresolvable": a default (uninitialized) <see cref="ImmutableArray{T}"/>.
    /// Every resolved state is built with <c>Create</c>/<c>ToImmutable</c> and is therefore never
    /// default, so <see cref="ImmutableArray{T}.IsDefault"/> distinguishes the two without a
    /// separate flag, sentinel instance or nullable wrapper.
    /// </summary>
    private static ImmutableArray<TableTransfer> Unresolvable => default;

    private readonly Dictionary<int, ImmutableArray<TableTransfer>> _executorResults;

    private DataTransferTableResolver(Dictionary<int, ImmutableArray<TableTransfer>> executorResults) =>
        _executorResults = executorResults;

    /// <summary>
    /// Walks a method or trigger body once and records the tables reaching every
    /// <c>DataTransfer</c> executor in it. Returns null when the body has no operation tree.
    /// </summary>
    public static DataTransferTableResolver? Create(
        SyntaxNode body,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (body is null || semanticModel is null)
            return null;

        var bodyOperation = semanticModel.GetOperation(body, cancellationToken);
        if (bodyOperation is null)
            return null;

        var walker = new Walker(semanticModel, cancellationToken);
        walker.Visit(bodyOperation);

        return new DataTransferTableResolver(walker.ExecutorResults);
    }

    /// <summary>
    /// Convenience overload for callers that hold only the executor: walks the method or
    /// trigger body the executor sits in. Returns null when the executor is not inside one.
    /// </summary>
    public static DataTransferTableResolver? CreateForEnclosingBody(
        IInvocationExpression executor,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (executor is null)
            return null;

        var body = executor.Syntax.FirstAncestorOrSelf<MethodOrTriggerDeclarationSyntax>()?.Body;
        return body is null ? null : Create(body, semanticModel, cancellationToken);
    }

    /// <summary>
    /// Gets the <c>(source, destination)</c> pairs the executor transfers.
    /// </summary>
    /// <returns>
    /// <c>false</c> when the tables are unresolvable: the executor was not reached by the walk,
    /// its receiver is neither a plain identifier nor <c>this.&lt;variable&gt;</c>, no
    /// <c>SetTables</c> reaches it on any path, or a <c>SetTables</c> that reaches it does not
    /// name both tables with a <c>Database::X</c> literal.
    /// </returns>
    public bool TryGetTables(IInvocationExpression executor, out ImmutableArray<TableTransfer> pairs)
    {
        if (executor is not null
            && _executorResults.TryGetValue(executor.Syntax.Span.Start, out var resolved)
            && !resolved.IsDefault)
        {
            pairs = resolved;
            return true;
        }

        pairs = ImmutableArray<TableTransfer>.Empty;
        return false;
    }

    private sealed class Walker : OperationWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Pending <c>SetTables</c> state per variable name on the current path. A missing key
        /// means nothing is pending; a default value means unresolvable.
        /// </summary>
        private readonly Dictionary<string, ImmutableArray<TableTransfer>> _state =
            new(SemanticFacts.NameEqualityComparer);

        /// <summary>
        /// States captured at <c>break</c> statements, one list per enclosing loop body pass.
        /// </summary>
        private readonly Stack<List<Dictionary<string, ImmutableArray<TableTransfer>>>> _breakStates = new();

        public Walker(SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            _semanticModel = semanticModel;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Result per executor, keyed by the start of its syntax span; a default value means
        /// unresolvable.
        /// </summary>
        public Dictionary<int, ImmutableArray<TableTransfer>> ExecutorResults { get; } = [];

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (operation.TargetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod
                && operation.Instance?.Type?.NavTypeKind == EnumProvider.NavTypeKind.DataTransfer)
            {
                var methodName = operation.TargetMethod.Name;

                if (SemanticFacts.IsSameName(methodName, DataTransferOperations.SetTablesMethodName))
                    RecordSetTables(operation);
                else if (DataTransferOperations.IsExecutor(methodName))
                    RecordExecutor(operation);
            }

            base.VisitInvocationExpression(operation);
        }

        /// <summary>
        /// A <c>SetTables</c> replaces whatever was pending for that variable on this path
        /// (strict reset), so each executor sees only the configuration that reaches it.
        /// </summary>
        private void RecordSetTables(IInvocationExpression operation)
        {
            var receiverName = GetReceiverIdentifierName(operation.Syntax);
            if (receiverName is null)
                return;

            _state[receiverName] =
                operation.Arguments.Length == 2
                && ResolveTableArgument(operation.Arguments[0].Value) is { } source
                && ResolveTableArgument(operation.Arguments[1].Value) is { } destination
                    ? ImmutableArray.Create<TableTransfer>((source, destination))
                    : Unresolvable;
        }

        /// <summary>
        /// An executor consumes the pending pairs but does not clear them: a second executor
        /// after the same <c>SetTables</c> transfers the same tables again. A later pass over a
        /// loop body overwrites the result with the state that also carries the loop's back edge.
        /// </summary>
        private void RecordExecutor(IInvocationExpression operation)
        {
            var receiverName = GetReceiverIdentifierName(operation.Syntax);

            ExecutorResults[operation.Syntax.Span.Start] =
                receiverName is not null && _state.TryGetValue(receiverName, out var pending)
                    ? pending
                    : Unresolvable;
        }

        #region Control flow overrides

        public override void VisitIfStatement(IIfStatement operation)
        {
            Visit(operation.Condition);

            var preBranchState = SaveState();

            Visit(operation.IfTrueStatement);
            var trueBranchState = SaveState();

            RestoreState(preBranchState);

            // IfFalseStatement is null when there is no else clause, despite the annotation.
            Visit(operation.IfFalseStatement);
            var falseBranchState = SaveState();

            Merge([trueBranchState, falseBranchState]);
        }

        public override void VisitCaseStatement(ICaseStatement operation)
        {
            Visit(operation.Value);

            var preCaseState = SaveState();
            var branchStates = new List<Dictionary<string, ImmutableArray<TableTransfer>>>();

            foreach (var caseLine in operation.CaseLines)
            {
                RestoreState(preCaseState);
                Visit(caseLine);
                branchStates.Add(SaveState());
            }

            if (operation.ElseStatement != null)
            {
                RestoreState(preCaseState);
                Visit(operation.ElseStatement);
                branchStates.Add(SaveState());
            }
            else
            {
                // No else clause: implicit empty branch carrying the pre-case state.
                branchStates.Add(preCaseState);
            }

            Merge(branchStates);
        }

        public override void VisitWhileRepeatLoopStatement(IWhileRepeatLoopStatement operation)
        {
            if (operation.LoopKind == EnumProvider.LoopKind.Repeat)
            {
                // repeat-until: the body always executes at least once, so the state from
                // before the loop is not on its own a possible state after it.
                VisitLoopBody(operation.Body, bodyAlwaysExecutes: true);
                Visit(operation.Condition);
            }
            else
            {
                Visit(operation.Condition);
                VisitLoopBody(operation.Body, bodyAlwaysExecutes: false);
            }
        }

        public override void VisitForLoopStatement(IForLoopStatement operation)
        {
            Visit(operation.InitialValue);
            Visit(operation.EndValue);
            VisitLoopBody(operation.Body, bodyAlwaysExecutes: false);
        }

        public override void VisitForEachLoopStatement(IForEachLoopStatement operation)
        {
            Visit(operation.Expression);
            VisitLoopBody(operation.Body, bodyAlwaysExecutes: false);
        }

        public override void VisitBreakStatement(IBreakStatement operation)
        {
            // `break` jumps to just after the loop, so the state here is one of the states the
            // code after the loop can see. Outside a loop there is nothing to contribute to.
            if (_breakStates.Count > 0)
                _breakStates.Peek().Add(SaveState());

            base.VisitBreakStatement(operation);
        }

        /// <summary>
        /// Visits a loop body twice, so that a <c>SetTables</c> written after an executor inside
        /// the body still reaches that executor through the loop's back edge. The merge is a
        /// union and every per-variable transfer function is either a constant or a
        /// pass-through, so one extra pass is the fixed point for a single loop; deeply nested
        /// loops could in principle need more.
        /// </summary>
        private void VisitLoopBody(IOperation loopBody, bool bodyAlwaysExecutes)
        {
            var preLoopState = SaveState();

            var firstPassBreaks = VisitLoopBodyOnce(loopBody);
            var afterFirstPass = SaveState();

            // The loop head, entered either from before the loop or from the end of the
            // previous pass. Starting the second pass here is what lets an executor inside the
            // body see the SetTables calls written after it, which reach it via the back edge.
            Merge([preLoopState, afterFirstPass]);

            var secondPassBreaks = VisitLoopBodyOnce(loopBody);
            var afterSecondPass = SaveState();

            // Leaving the loop: falling out of the body after either pass, or any `break`.
            // A loop whose body may not run at all can also be left with the pre-loop state.
            var exitStates = new List<Dictionary<string, ImmutableArray<TableTransfer>>>(firstPassBreaks);
            exitStates.AddRange(secondPassBreaks);
            exitStates.Add(afterFirstPass);
            exitStates.Add(afterSecondPass);
            if (!bodyAlwaysExecutes)
                exitStates.Add(preLoopState);

            Merge(exitStates);
        }

        /// <summary>
        /// Visits the body once and returns the states captured at the <c>break</c> statements
        /// that belong to this loop (a stack, so nested loops keep theirs apart).
        /// </summary>
        private List<Dictionary<string, ImmutableArray<TableTransfer>>> VisitLoopBodyOnce(IOperation loopBody)
        {
            _breakStates.Push([]);
            Visit(loopBody);
            return _breakStates.Pop();
        }

        #endregion

        #region Flow state management

        private Dictionary<string, ImmutableArray<TableTransfer>> SaveState() =>
            new(_state, SemanticFacts.NameEqualityComparer);

        private void RestoreState(Dictionary<string, ImmutableArray<TableTransfer>> saved)
        {
            // Cleared first: unlike PC0030 the keys are discovered during the walk instead of
            // being pre-seeded, so a branch can introduce keys the saved state never had.
            _state.Clear();
            foreach (var kvp in saved)
                _state[kvp.Key] = kvp.Value;
        }

        /// <summary>
        /// Merges the branch states: a variable is unresolvable when any branch left it
        /// unresolvable, and its pairs are the union over all branches (deduped by table id).
        /// </summary>
        private void Merge(List<Dictionary<string, ImmutableArray<TableTransfer>>> branchStates)
        {
            var merged = new Dictionary<string, ImmutableArray<TableTransfer>>(SemanticFacts.NameEqualityComparer);

            foreach (var branchState in branchStates)
            {
                foreach (var key in branchState.Keys)
                {
                    if (!merged.ContainsKey(key))
                        merged[key] = MergeVariable(key, branchStates);
                }
            }

            RestoreState(merged);
        }

        private static ImmutableArray<TableTransfer> MergeVariable(
            string key,
            List<Dictionary<string, ImmutableArray<TableTransfer>>> branchStates)
        {
            var pairs = ImmutableArray.CreateBuilder<TableTransfer>();
            var seen = new HashSet<(int Source, int Destination)>();

            foreach (var branchState in branchStates)
            {
                // A branch that never touched this variable contributes an empty state; that is
                // the correct reading here, because a state entry only exists once a SetTables
                // on that path created it.
                if (!branchState.TryGetValue(key, out var pending))
                    continue;

                if (pending.IsDefault)
                    return Unresolvable;

                foreach (var pair in pending)
                {
                    if (seen.Add((pair.Source.Id, pair.Destination.Id)))
                        pairs.Add(pair);
                }
            }

            return pairs.ToImmutable();
        }

        #endregion

        /// <summary>
        /// Names the variable a member-access call is made on, from either syntax form: with
        /// parentheses (<c>dt.CopyFields()</c>) or without (<c>dt.CopyFields;</c>), and whether the
        /// variable is addressed bare (<c>dt</c>) or through the self-reference (<c>this.dt</c>).
        /// Both forms yield the bare variable name, so a <c>SetTables</c> written one way still
        /// matches an executor written the other. Returns null for any other receiver, which puts
        /// the DataTransfer variable out of reach of the same-body <c>SetTables</c> lookup.
        /// </summary>
        private string? GetReceiverIdentifierName(SyntaxNode syntax)
        {
            if (!syntax.TryGetMethodCall(out _, out var receiver, out _))
                return null;

            if (receiver is IdentifierNameSyntax identifier)
                return identifier.Identifier.ValueText?.UnquoteIdentifier();

            // `this.MyDataTransfer.CopyFields()`: the variable sits one level below the receiver.
            // The self-reference is recognized through the operation tree, NOT ThisExpressionSyntax
            // or SyntaxKind.ThisExpression: both are absent from the netstandard2.1 compile floor
            // (see .claude/rules/netstandard21-compatibility.md). The OperationKind member resolves
            // to default on SDKs without it, where no `this` code can exist anyway.
            var thisReferenceKind = EnumProvider.OperationKind.ThisReference;
            if (thisReferenceKind != default
                && receiver is MemberAccessExpressionSyntax qualified
                && _semanticModel.GetOperation(qualified.Expression, _cancellationToken)?.Kind == thisReferenceKind)
                return qualified.Name.Identifier.ValueText?.UnquoteIdentifier();

            return null;
        }

        /// <summary>
        /// Resolves a <c>SetTables</c> argument to the table it names. Only object-access literals
        /// (<c>Database::"My Table"</c>) resolve; integer variables and expressions do not.
        /// </summary>
        private static ITableTypeSymbol? ResolveTableArgument(IOperation argument) =>
            argument.UnwrapConversions().GetSymbolSafe() as ITableTypeSymbol;
    }
}
