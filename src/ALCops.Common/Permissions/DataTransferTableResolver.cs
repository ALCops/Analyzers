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
/// Resolves the tables a <c>DataTransfer</c> executor (<c>CopyFields</c>/<c>CopyRows</c>)
/// transfers, by walking the enclosing method or trigger body in flow order.
/// <para>
/// Per <c>DataTransfer</c> variable: a <c>SetTables</c> replaces the pending pair on the path,
/// an executor consumes the pending pairs without clearing them, and branches merge as a union
/// (<c>break</c> included). Loop bodies are visited twice so a <c>SetTables</c> written after an
/// executor still reaches it through the back edge.
/// </para>
/// <para>
/// Accepted imprecision: <c>exit</c> does not terminate a path, so state from before an early
/// exit still flows forward. That over-attributes, which is conservative for AC0032 but can make
/// AC0031 ask for a permission the code does not need.
/// </para>
/// </summary>
public sealed class DataTransferTableResolver
{
    /// <summary>The one encoding of "unresolvable"; a resolved state is never default.</summary>
    private static ImmutableArray<TableTransfer> Unresolvable => default;

    private readonly Dictionary<int, ImmutableArray<TableTransfer>> _executorResults;

    private DataTransferTableResolver(Dictionary<int, ImmutableArray<TableTransfer>> executorResults) =>
        _executorResults = executorResults;

    /// <summary>Walks a method or trigger body once; null when it has no operation tree.</summary>
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

    /// <summary>Walks the body the executor sits in; null when it is not inside one.</summary>
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

    /// <summary>Gets the <c>(source, destination)</c> pairs the executor transfers.</summary>
    /// <returns>
    /// <c>false</c> when unresolvable: the executor was not reached by the walk, its receiver is
    /// neither a plain identifier nor <c>this.&lt;variable&gt;</c>, no <c>SetTables</c> reaches
    /// it, or one that does names a table with something other than a <c>Database::X</c> literal.
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

        /// <summary>Pending pairs per variable; key missing = none, default value = unresolvable.</summary>
        private readonly Dictionary<string, ImmutableArray<TableTransfer>> _state =
            new(SemanticFacts.NameEqualityComparer);

        /// <summary>States captured at <c>break</c>, one list per loop body pass in progress.</summary>
        private readonly Stack<List<Dictionary<string, ImmutableArray<TableTransfer>>>> _breakStates = new();

        public Walker(SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            _semanticModel = semanticModel;
            _cancellationToken = cancellationToken;
        }

        /// <summary>Keyed by the executor's syntax span start; a default value is unresolvable.</summary>
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

        /// <summary>Strict reset: replaces whatever was pending for that variable on this path.</summary>
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

        /// <summary>Consumes the pending pairs without clearing them; a later loop pass overwrites.</summary>
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
            // The state at the jump is one the code after the loop sees; ignored outside a loop.
            if (_breakStates.Count > 0)
                _breakStates.Peek().Add(SaveState());

            base.VisitBreakStatement(operation);
        }

        /// <summary>
        /// Two passes: the second starts at the loop head, so a <c>SetTables</c> written after an
        /// executor still reaches it through the back edge. Constant-or-passthrough transfer
        /// functions over a union lattice make that the fixed point for a single loop.
        /// </summary>
        private void VisitLoopBody(IOperation loopBody, bool bodyAlwaysExecutes)
        {
            var preLoopState = SaveState();

            var firstPassBreaks = VisitLoopBodyOnce(loopBody);
            var afterFirstPass = SaveState();

            // Loop head: entered from before the loop, or from the end of the previous pass.
            Merge([preLoopState, afterFirstPass]);

            var secondPassBreaks = VisitLoopBodyOnce(loopBody);
            var afterSecondPass = SaveState();

            // Leaving: falling out of either pass, any break, or never entering the body.
            var exitStates = new List<Dictionary<string, ImmutableArray<TableTransfer>>>(firstPassBreaks);
            exitStates.AddRange(secondPassBreaks);
            exitStates.Add(afterFirstPass);
            exitStates.Add(afterSecondPass);
            if (!bodyAlwaysExecutes)
                exitStates.Add(preLoopState);

            Merge(exitStates);
        }

        /// <summary>Returns this pass's <c>break</c> states; the stack keeps nested loops apart.</summary>
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
            // Cleared first: keys appear during the walk, so a branch can add keys saved lacks.
            _state.Clear();
            foreach (var kvp in saved)
                _state[kvp.Key] = kvp.Value;
        }

        /// <summary>Union of the branch pairs per variable; unresolvable if any branch is.</summary>
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
                // A branch without the key has nothing pending, not "unchanged".
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
        /// The variable a call is made on, bare (<c>dt.CopyFields()</c>) or self-qualified
        /// (<c>this.dt.CopyFields</c>); both yield the bare name, so a <c>SetTables</c> written
        /// one way matches an executor written the other. Null for any other receiver.
        /// </summary>
        private string? GetReceiverIdentifierName(SyntaxNode syntax)
        {
            if (!syntax.TryGetMethodCall(out _, out var receiver, out _))
                return null;

            if (receiver is IdentifierNameSyntax identifier)
                return identifier.Identifier.ValueText?.UnquoteIdentifier();

            // Self-reference via the operation tree, never ThisExpressionSyntax /
            // SyntaxKind.ThisExpression: both are absent at the netstandard2.1 compile floor
            // (.claude/rules/netstandard21-compatibility.md).
            var thisReferenceKind = EnumProvider.OperationKind.ThisReference;
            if (thisReferenceKind != default
                && receiver is MemberAccessExpressionSyntax qualified
                && _semanticModel.GetOperation(qualified.Expression, _cancellationToken)?.Kind == thisReferenceKind)
                return qualified.Name.Identifier.ValueText?.UnquoteIdentifier();

            return null;
        }

        /// <summary>Only <c>Database::"X"</c> object-access literals resolve.</summary>
        private static ITableTypeSymbol? ResolveTableArgument(IOperation argument) =>
            argument.UnwrapConversions().GetSymbolSafe() as ITableTypeSymbol;
    }
}
