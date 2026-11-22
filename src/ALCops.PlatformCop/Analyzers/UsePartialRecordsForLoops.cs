using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ALCops.Common;
using ALCops.Common.Extensions;
using ALCops.Common.Helpers;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.PlatformCop.Analyzers;

/// <summary>
/// Reports a read on a local record that drives a <c>Next()</c> loop without partial records.
/// A read is "pending" until a built-in <c>Next</c> on the same variable runs inside a loop that
/// does not contain the read; it is then "confirmed" and reported unless a later full-record
/// operation on the same path clears it.
/// </summary>
[DiagnosticAnalyzer]
public sealed class UsePartialRecordsForLoops : DiagnosticAnalyzer
{
    /// <summary>
    /// Reads that position the record and fill its buffer, so a later <c>Next()</c> loop iterates from them.
    /// Evidence-based: every entry method measured gave the same SetLoadFields gain once a
    /// <c>Next()</c> loop followed, and none gained without the loop, so the loop is the signal and
    /// the entry method is not. IsEmpty and Count do not fill the buffer.
    /// </summary>
    private static readonly ImmutableHashSet<string> LoopEntryReadMethods =
        ImmutableHashSet.Create(
            SemanticFacts.NameEqualityComparer,
            "Find", "FindFirst", "FindLast", "FindSet", "Get", "GetBySystemId");

    private const string NextMethodName = "Next";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.UsePartialRecordsForLoops);

    // SetLoadFields was introduced in runtime 6.0 (BC17, Fall 2020); Spring2021OrGreater (BC18)
    // is the closest version guard available.
    public override VersionCompatibility SupportedVersions =>
        VersionProvider.VersionCompatibility.Spring2021OrGreater;

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCodeBlockAction(AnalyzeCodeBlock);

    private static void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
    {
        if (context.IsObsolete() ||
            context.CodeBlock is not MethodOrTriggerDeclarationSyntax methodOrTrigger)
            return;

        if (context.OwningSymbol is not IMethodSymbol methodSymbol)
            return;

        var body = methodOrTrigger.Body;
        if (body is null)
            return;

        var trackedVariables = CollectTrackedLocalVariables(methodSymbol);
        if (trackedVariables.Count == 0)
            return;

        // Without a Next call there is no loop read; skip the bind.
        if (!ContainsNextIdentifier(body))
            return;

        var operation = context.SemanticModel.GetOperation(body, context.CancellationToken);
        if (operation is null)
            return;

        var walker = new LoopReadWalker(trackedVariables, context.CancellationToken);
        walker.Visit(operation);

        foreach (var kvp in walker.FlowState)
        {
            var reported = new HashSet<int>();
            foreach (var read in kvp.Value.ConfirmedReads)
            {
                if (!reported.Add(read.Location.SourceSpan.Start))
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UsePartialRecordsForLoops,
                    read.Location,
                    kvp.Key, read.MethodName));
            }
        }
    }

    private static bool ContainsNextIdentifier(SyntaxNode body)
    {
        foreach (var token in body.DescendantTokens())
        {
            if (token.Kind == EnumProvider.SyntaxKind.IdentifierToken &&
                token.ValueText?.UnquoteIdentifier().IsSameName(NextMethodName) == true)
                return true;
        }

        return false;
    }

    private static HashSet<string> CollectTrackedLocalVariables(IMethodSymbol methodSymbol)
    {
        var result = new HashSet<string>(SemanticFacts.NameEqualityComparer);

        foreach (var local in methodSymbol.LocalVariables)
        {
            if (IsTrackedVariable(local))
                result.Add(local.Name);
        }

        return result;
    }

    private static bool IsTrackedVariable(IVariableSymbol variable)
    {
        var type = variable.Type;
        if (type is null)
            return false;

        if (type is IRecordTypeSymbol recordType)
        {
            if (recordType.Temporary)
                return false;

            if (recordType.OriginalDefinition is not ITableTypeSymbol tableType)
                return true;

            // Setup and reference tables stay in the NST record cache; SetLoadFields bypasses it.
            return tableType.TableType == EnumProvider.TableTypeKind.Normal &&
                   !TableHelper.IsSetupOrReferenceTable(tableType);
        }

        return type.NavTypeKind == EnumProvider.NavTypeKind.RecordRef;
    }

    /// <summary>
    /// Per-variable flow state, forked at branch points and merged at join points.
    /// </summary>
    private sealed class FlowFlags
    {
        public bool HasLoadFields { get; set; }
        public bool HasFullRecordAccess { get; set; }
        public bool PassedToFunction { get; set; }

        /// <summary>Reads that may still drive a later Next() loop.</summary>
        public List<ReadInfo> PendingReads { get; set; } = new();

        /// <summary>Reads that drove a Next() loop on this path and are reported unless cleared.</summary>
        public List<ReadInfo> ConfirmedReads { get; set; } = new();

        public bool BlocksNewReads => HasLoadFields || HasFullRecordAccess || PassedToFunction;

        public FlowFlags Clone() => new()
        {
            HasLoadFields = HasLoadFields,
            HasFullRecordAccess = HasFullRecordAccess,
            PassedToFunction = PassedToFunction,
            PendingReads = new List<ReadInfo>(PendingReads),
            ConfirmedReads = new List<ReadInfo>(ConfirmedReads)
        };

        public void ResetFlags()
        {
            HasLoadFields = false;
            HasFullRecordAccess = false;
            PassedToFunction = false;
        }

        /// <summary>
        /// A full-record operation on this path: adding SetLoadFields before an earlier read could
        /// break it, so both lists are cleared retroactively.
        /// </summary>
        public void ClearReads()
        {
            PendingReads.Clear();
            ConfirmedReads.Clear();
        }
    }

    private sealed class PreForkReads(HashSet<int> pending, HashSet<int> confirmed)
    {
        public HashSet<int> Pending { get; } = pending;
        public HashSet<int> Confirmed { get; } = confirmed;
    }

#if NETSTANDARD2_1
    private readonly struct ReadInfo
    {
        public Location Location { get; }
        public string MethodName { get; }
        public int LoopDepth { get; }

        public ReadInfo(Location location, string methodName, int loopDepth)
        {
            Location = location;
            MethodName = methodName;
            LoopDepth = loopDepth;
        }
    }
#else
    private readonly record struct ReadInfo(Location Location, string MethodName, int LoopDepth);
#endif

    private sealed class LoopReadWalker : OperationWalker
    {
        private readonly CancellationToken _cancellationToken;
        private readonly Dictionary<string, FlowFlags> _flowState;

        /// <summary>
        /// Number of loops (repeat, while, for, foreach; condition and body) enclosing the current operation.
        /// A Next call confirms only reads made outside the loop it runs in.
        /// </summary>
        private int _loopDepth;

        public LoopReadWalker(HashSet<string> trackedVariables, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _flowState = new Dictionary<string, FlowFlags>(SemanticFacts.NameEqualityComparer);
            foreach (var name in trackedVariables)
                _flowState[name] = new FlowFlags();
        }

        public IReadOnlyDictionary<string, FlowFlags> FlowState => _flowState;

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var instanceSymbol = operation.Instance?.GetSymbolSafe();

            // The map holds locals only; `this.<Global>` or `this.<DataItem>` binds to
            // another symbol that may share the name.
            if (instanceSymbol?.Kind == EnumProvider.SymbolKind.LocalVariable &&
                _flowState.TryGetValue(instanceSymbol.Name, out var flowFlags))
            {
                ClassifyInstanceMethodCall(operation, flowFlags);
            }

            CheckArgumentsForTrackedVariables(operation);

            base.VisitInvocationExpression(operation);
        }

        public override void VisitAssignmentStatement(IAssignmentStatement operation)
        {
            // TempMyTable := MyTable copies every field of the source.
            if (TryGetFlowFlags(operation.Value, out var flowFlags))
            {
                flowFlags.ClearReads();
                flowFlags.HasFullRecordAccess = true;
            }

            base.VisitAssignmentStatement(operation);
        }

        public override void VisitExitStatement(IExitStatement operation)
        {
            // exit(MyTable): the caller may read any field. The path ends here, so no forward
            // flag is set; one would leak past the enclosing branch through the OR-merge.
            if (TryGetFlowFlags(operation.ReturnedValue, out var flowFlags))
                flowFlags.ClearReads();

            base.VisitExitStatement(operation);
        }

        #region Control flow overrides

        public override void VisitIfStatement(IIfStatement operation)
        {
            Visit(operation.Condition);

            var preFork = CapturePreForkReads();
            var preBranchState = SaveFlowState();

            Visit(operation.IfTrueStatement);
            var trueBranchState = SaveFlowState();

            RestoreFlowState(preBranchState);
            Visit(operation.IfFalseStatement);
            var falseBranchState = SaveFlowState();

            MergeFlowStates([trueBranchState, falseBranchState], preFork);
        }

        public override void VisitCaseStatement(ICaseStatement operation)
        {
            Visit(operation.Value);

            var preFork = CapturePreForkReads();
            var preCaseState = SaveFlowState();
            var branchStates = new List<Dictionary<string, FlowFlags>>();

            foreach (var caseLine in operation.CaseLines)
            {
                RestoreFlowState(preCaseState);
                Visit(caseLine);
                branchStates.Add(SaveFlowState());
            }

            if (operation.ElseStatement != null)
            {
                RestoreFlowState(preCaseState);
                Visit(operation.ElseStatement);
                branchStates.Add(SaveFlowState());
            }
            else
            {
                // No else clause: implicit empty branch. preCaseState is a clone that is not
                // restored again, so it can be used directly.
                branchStates.Add(preCaseState);
            }

            MergeFlowStates(branchStates, preFork);
        }

        public override void VisitWhileRepeatLoopStatement(IWhileRepeatLoopStatement operation)
        {
            _loopDepth++;
            if (operation.LoopKind == EnumProvider.LoopKind.Repeat)
            {
                // repeat-until: the body always executes at least once.
                Visit(operation.Body);
                Visit(operation.Condition);
            }
            else
            {
                Visit(operation.Condition);
                VisitConditionalLoopBody(operation.Body);
            }
            _loopDepth--;
        }

        public override void VisitForLoopStatement(IForLoopStatement operation)
        {
            _loopDepth++;
            Visit(operation.InitialValue);
            Visit(operation.EndValue);
            VisitConditionalLoopBody(operation.Body);
            _loopDepth--;
        }

        public override void VisitForEachLoopStatement(IForEachLoopStatement operation)
        {
            _loopDepth++;
            Visit(operation.Expression);
            VisitConditionalLoopBody(operation.Body);
            _loopDepth--;
        }

        /// <summary>
        /// A while, for or foreach body may not execute: fork before it and merge with the pre-loop state.
        /// Single pass, no fixed point.
        /// </summary>
        private void VisitConditionalLoopBody(IOperation loopBody)
        {
            var preFork = CapturePreForkReads();
            var preLoopState = SaveFlowState();
            Visit(loopBody);
            var postBodyState = SaveFlowState();

            MergeFlowStates([preLoopState, postBodyState], preFork);
        }

        #endregion

        #region Flow state management

        private Dictionary<string, FlowFlags> SaveFlowState()
        {
            var saved = new Dictionary<string, FlowFlags>(SemanticFacts.NameEqualityComparer);
            foreach (var kvp in _flowState)
                saved[kvp.Key] = kvp.Value.Clone();
            return saved;
        }

        private void RestoreFlowState(Dictionary<string, FlowFlags> saved)
        {
            foreach (var kvp in saved)
                _flowState[kvp.Key] = kvp.Value.Clone();
        }

        private Dictionary<string, PreForkReads> CapturePreForkReads()
        {
            var result = new Dictionary<string, PreForkReads>(SemanticFacts.NameEqualityComparer);
            foreach (var kvp in _flowState)
            {
                if (kvp.Value.PendingReads.Count == 0 && kvp.Value.ConfirmedReads.Count == 0)
                    continue;

                result[kvp.Key] = new PreForkReads(
                    new HashSet<int>(kvp.Value.PendingReads.Select(r => r.Location.SourceSpan.Start)),
                    new HashSet<int>(kvp.Value.ConfirmedReads.Select(r => r.Location.SourceSpan.Start)));
            }
            return result;
        }

        private void MergeFlowStates(List<Dictionary<string, FlowFlags>> branchStates,
            Dictionary<string, PreForkReads> preForkReads)
        {
            foreach (var kvp in _flowState)
            {
                var name = kvp.Key;
                var flags = kvp.Value;
                flags.HasLoadFields = branchStates.All(bs => bs[name].HasLoadFields);
                flags.HasFullRecordAccess = branchStates.Any(bs => bs[name].HasFullRecordAccess);
                flags.PassedToFunction = branchStates.Any(bs => bs[name].PassedToFunction);

                preForkReads.TryGetValue(name, out var preFork);
                flags.PendingReads = MergeReads(branchStates.Select(bs => bs[name].PendingReads).ToList(), preFork?.Pending);
                flags.ConfirmedReads = MergeReads(branchStates.Select(bs => bs[name].ConfirmedReads).ToList(), preFork?.Confirmed);
            }
        }

        /// <summary>
        /// Reads added inside a branch are unioned; a read that existed before the fork survives only
        /// when every branch kept it (a branch that cleared it retroactively wins). Deduplicated by
        /// position, otherwise nested forks double the list at each level.
        /// </summary>
        private static List<ReadInfo> MergeReads(List<List<ReadInfo>> branchReads, HashSet<int>? preFork)
        {
            var merged = new List<ReadInfo>();
            var seen = new HashSet<int>();
            foreach (var reads in branchReads)
            {
                foreach (var read in reads)
                {
                    var start = read.Location.SourceSpan.Start;
                    if (!seen.Add(start))
                        continue;

                    if (preFork is not null && preFork.Contains(start) &&
                        !branchReads.All(other => other.Any(r => r.Location.SourceSpan.Start == start)))
                        continue;

                    merged.Add(read);
                }
            }
            return merged;
        }

        #endregion

        #region Method classification

        private void ClassifyInstanceMethodCall(IInvocationExpression operation, FlowFlags flowFlags)
        {
            var methodName = operation.TargetMethod.Name;

            if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            {
                // Table procedure called on the record: the callee may access any field.
                flowFlags.ClearReads();
                flowFlags.PassedToFunction = true;
                return;
            }

            if (LoopEntryReadMethods.Contains(methodName))
                HandleEntryRead(operation, flowFlags, methodName);
            else if (SemanticFacts.IsSameName(methodName, NextMethodName))
                ConfirmPendingReads(flowFlags);
            else if (RecordMethodClassification.LoadFieldsMethods.Contains(methodName))
                flowFlags.HasLoadFields = !(SemanticFacts.IsSameName(methodName, "SetLoadFields") && operation.Arguments.IsEmpty);
            else if (RecordMethodClassification.WriteMethods.Contains(methodName) || IsRecordRefSetTable(operation, methodName))
            {
                flowFlags.ClearReads();
                flowFlags.HasFullRecordAccess = true;
            }
            else if (SemanticFacts.IsSameName(methodName, "Reset"))
                flowFlags.ResetFlags();
        }

        /// <summary>
        /// A read repositions the cursor: earlier unconfirmed reads at this loop level or deeper no
        /// longer drive a later loop. A read inside a loop body never discards the driver outside it.
        /// </summary>
        private void HandleEntryRead(IInvocationExpression operation, FlowFlags flowFlags, string methodName)
        {
            var depth = _loopDepth;
            flowFlags.PendingReads.RemoveAll(r => r.LoopDepth >= depth);

            if (!flowFlags.BlocksNewReads)
                flowFlags.PendingReads.Add(new ReadInfo(operation.Syntax.GetLocation(), methodName, depth));
        }

        /// <summary>
        /// A Next inside a loop confirms the reads made outside that loop. A Next outside every loop
        /// (depth 0) confirms nothing.
        /// </summary>
        private void ConfirmPendingReads(FlowFlags flowFlags)
        {
            var depth = _loopDepth;
            if (depth == 0)
                return;

            foreach (var read in flowFlags.PendingReads)
            {
                if (read.LoopDepth < depth)
                    flowFlags.ConfirmedReads.Add(read);
            }
            flowFlags.PendingReads.RemoveAll(r => r.LoopDepth < depth);
        }

        /// <summary>
        /// RecordRef.SetTable copies the RecordRef into a record that may then be used in full.
        /// Treated as a full-record operation regardless of the target.
        /// </summary>
        private static bool IsRecordRefSetTable(IInvocationExpression operation, string methodName) =>
            SemanticFacts.IsSameName(methodName, "SetTable") &&
            operation.Instance?.Type?.NavTypeKind == EnumProvider.NavTypeKind.RecordRef;

        private void CheckArgumentsForTrackedVariables(IInvocationExpression operation)
        {
            // Clear(MyTable) reinitializes the variable: a cleared cursor cannot drive a later loop.
            if (operation.TargetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod &&
                SemanticFacts.IsSameName(operation.TargetMethod.Name, "Clear") &&
                operation.Arguments.Length >= 1 &&
                TryGetFlowFlags(operation.Arguments[0].Value, out var clearFlags))
            {
                clearFlags.ResetFlags();
                clearFlags.PendingReads.Clear();
                return;
            }

            foreach (var argument in operation.Arguments)
            {
                // The callee may access any field.
                if (TryGetFlowFlags(argument.Value, out var flags))
                {
                    flags.ClearReads();
                    flags.PassedToFunction = true;
                }
            }
        }

        #endregion

        private bool TryGetFlowFlags(IOperation? value, [NotNullWhen(true)] out FlowFlags? flowFlags)
        {
            var name = GetVariableNameFromOperation(value);
            flowFlags = null;
            return name is not null && _flowState.TryGetValue(name, out flowFlags);
        }

        /// <summary>
        /// Name of a bare variable reference (optionally behind an implicit conversion); field access
        /// and other expressions return null. A bare identifier binds innermost, so a local name cannot
        /// alias an object-scope variable here.
        /// </summary>
        private static string? GetVariableNameFromOperation(IOperation? value)
        {
            if (value is IConversionExpression conversion)
            {
                if (conversion.Syntax is IdentifierNameSyntax convIdent)
                    return convIdent.Identifier.ValueText?.UnquoteIdentifier();

                value = conversion.Operand;
            }

            if (value?.Syntax is IdentifierNameSyntax directIdent)
                return directIdent.Identifier.ValueText?.UnquoteIdentifier();

            return null;
        }
    }
}
