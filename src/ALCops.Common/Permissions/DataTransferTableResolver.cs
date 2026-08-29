using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.Common.Permissions;

/// <summary>
/// Resolves which tables a <c>DataTransfer</c> executor (<c>CopyFields</c>/<c>CopyRows</c>)
/// actually transfers, by walking the enclosing method or trigger body in flow order and
/// pairing every executor with the <c>SetTables(Database::X, Database::Y)</c> call that
/// reaches it.
/// <para>
/// State is kept per <c>DataTransfer</c> variable (by the name the receiver resolves to).
/// A <c>SetTables</c> <em>replaces</em> the pending pair for that variable on the current path,
/// because AL code commonly reconfigures one transfer variable for several sequential copies.
/// An executor consumes the pending pairs without clearing them, so
/// <c>SetTables; CopyRows; CopyFields</c> attributes both executors to the same pair.
/// Branches fork and merge like PC0030's <c>SetLoadFieldsWalker</c>; a merge is the
/// <em>union</em> of the branches' pending pairs, which keeps the result conservative
/// (it can only add tables, never drop one that a path really transfers).
/// </para>
/// <para>
/// Accepted limitations: <c>exit</c> does not terminate a path, so state from before an early
/// exit still flows forward (over-attribution, the safe direction); and the walk is a single
/// pass, so a <c>SetTables</c> written after an executor inside a loop body does not flow back
/// to that executor — the executor is then unresolvable unless another <c>SetTables</c>
/// precedes it, which is again the safe direction.
/// </para>
/// </summary>
public sealed class DataTransferTableResolver
{
    private readonly Dictionary<int, PendingTables?> _executorResults;

    private DataTransferTableResolver(Dictionary<int, PendingTables?> executorResults) =>
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
    public bool TryGetTables(
        IInvocationExpression executor,
        out ImmutableArray<(ITableTypeSymbol Source, ITableTypeSymbol Destination)> pairs)
    {
        if (executor is not null
            && _executorResults.TryGetValue(executor.Syntax.Span.Start, out var pending)
            && pending is not null)
        {
            pairs = pending.Pairs;
            return true;
        }

        pairs = ImmutableArray<(ITableTypeSymbol Source, ITableTypeSymbol Destination)>.Empty;
        return false;
    }

    /// <summary>
    /// Immutable pending state of one <c>DataTransfer</c> variable on the current path.
    /// Being immutable makes save/restore of the walker state a shallow dictionary copy.
    /// </summary>
    private sealed class PendingTables
    {
        public static readonly PendingTables Unresolvable = new(true,
            ImmutableArray<(ITableTypeSymbol Source, ITableTypeSymbol Destination)>.Empty);

        public PendingTables(bool isUnresolvable,
            ImmutableArray<(ITableTypeSymbol Source, ITableTypeSymbol Destination)> pairs)
        {
            IsUnresolvable = isUnresolvable;
            Pairs = pairs;
        }

        public bool IsUnresolvable { get; }

        public ImmutableArray<(ITableTypeSymbol Source, ITableTypeSymbol Destination)> Pairs { get; }
    }

    private sealed class Walker : OperationWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly CancellationToken _cancellationToken;

        /// <summary>Pending <c>SetTables</c> state per variable name on the current path.</summary>
        private readonly Dictionary<string, PendingTables> _state =
            new(SemanticFacts.NameEqualityComparer);

        public Walker(SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            _semanticModel = semanticModel;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Result per executor, keyed by the start of its syntax span; a null value means
        /// unresolvable.
        /// </summary>
        public Dictionary<int, PendingTables?> ExecutorResults { get; } = [];

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
            var receiverName = GetReceiverIdentifierName(operation.Syntax, _semanticModel);
            if (receiverName is null)
                return;

            if (operation.Arguments.Length == 2
                && ResolveTableArgument(operation.Arguments[0].Value) is { } source
                && ResolveTableArgument(operation.Arguments[1].Value) is { } destination)
            {
                _state[receiverName] = new PendingTables(false, ImmutableArray.Create((source, destination)));
            }
            else
            {
                _state[receiverName] = PendingTables.Unresolvable;
            }
        }

        /// <summary>
        /// An executor consumes the pending pairs but does not clear them: a second executor
        /// after the same <c>SetTables</c> transfers the same tables again.
        /// </summary>
        private void RecordExecutor(IInvocationExpression operation)
        {
            var receiverName = GetReceiverIdentifierName(operation.Syntax, _semanticModel);

            ExecutorResults[operation.Syntax.Span.Start] =
                receiverName is not null
                && _state.TryGetValue(receiverName, out var pending)
                && !pending.IsUnresolvable
                && !pending.Pairs.IsEmpty
                    ? pending
                    : null;
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
            var branchStates = new List<Dictionary<string, PendingTables>>();

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
                // repeat-until: the body always executes at least once, so no merge is needed.
                Visit(operation.Body);
                Visit(operation.Condition);
            }
            else
            {
                Visit(operation.Condition);
                VisitConditionalLoopBody(operation.Body);
            }
        }

        public override void VisitForLoopStatement(IForLoopStatement operation)
        {
            Visit(operation.InitialValue);
            Visit(operation.EndValue);
            VisitConditionalLoopBody(operation.Body);
        }

        public override void VisitForEachLoopStatement(IForEachLoopStatement operation)
        {
            Visit(operation.Expression);
            VisitConditionalLoopBody(operation.Body);
        }

        /// <summary>
        /// Loop bodies that may not execute (while-do, for, foreach): merge the pre-loop state
        /// with the state after one pass through the body.
        /// </summary>
        private void VisitConditionalLoopBody(IOperation loopBody)
        {
            var preLoopState = SaveState();
            Visit(loopBody);
            var postBodyState = SaveState();

            Merge([preLoopState, postBodyState]);
        }

        #endregion

        #region Flow state management

        private Dictionary<string, PendingTables> SaveState() =>
            new(_state, SemanticFacts.NameEqualityComparer);

        private void RestoreState(Dictionary<string, PendingTables> saved)
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
        private void Merge(List<Dictionary<string, PendingTables>> branchStates)
        {
            var merged = new Dictionary<string, PendingTables>(SemanticFacts.NameEqualityComparer);

            foreach (var branchState in branchStates)
            {
                foreach (var key in branchState.Keys)
                {
                    if (!merged.ContainsKey(key))
                        merged[key] = MergeVariable(key, branchStates);
                }
            }

            _state.Clear();
            foreach (var kvp in merged)
                _state[kvp.Key] = kvp.Value;
        }

        private static PendingTables MergeVariable(
            string key,
            List<Dictionary<string, PendingTables>> branchStates)
        {
            var unresolvable = false;
            var pairs = ImmutableArray.CreateBuilder<(ITableTypeSymbol Source, ITableTypeSymbol Destination)>();
            var seen = new HashSet<(int Source, int Destination)>();

            foreach (var branchState in branchStates)
            {
                // A branch that never touched this variable contributes an empty state; that is
                // the correct reading here, because a state entry only exists once a SetTables
                // on that path created it.
                if (!branchState.TryGetValue(key, out var pending))
                    continue;

                unresolvable |= pending.IsUnresolvable;

                foreach (var pair in pending.Pairs)
                {
                    if (seen.Add((pair.Source.Id, pair.Destination.Id)))
                        pairs.Add(pair);
                }
            }

            return unresolvable ? PendingTables.Unresolvable : new PendingTables(false, pairs.ToImmutable());
        }

        #endregion
    }

    /// <summary>
    /// Names the variable a member-access call is made on, from either syntax form: with
    /// parentheses (<c>dt.CopyFields()</c>) or without (<c>dt.CopyFields;</c>), and whether the
    /// variable is addressed bare (<c>dt</c>) or through the self-reference (<c>this.dt</c>).
    /// Both forms yield the bare variable name, so a <c>SetTables</c> written one way still
    /// matches an executor written the other. Returns null for any other receiver, which puts
    /// the DataTransfer variable out of reach of the same-body <c>SetTables</c> lookup.
    /// </summary>
    private static string? GetReceiverIdentifierName(SyntaxNode syntax, SemanticModel semanticModel)
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
            && semanticModel.GetOperation(qualified.Expression)?.Kind == thisReferenceKind)
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
