using System.Collections.Immutable;
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

[DiagnosticAnalyzer]
public sealed class PartialRecordOperations : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> ReadMethods = RecordMethodClassification.SingleRecordReadMethods
        .Union(ImmutableHashSet.Create(SemanticFacts.NameEqualityComparer, "FindSet"));

    private static readonly ImmutableHashSet<string> LoadFieldsMethods = RecordMethodClassification.LoadFieldsMethods;

    private static readonly ImmutableHashSet<string> WriteMethods = RecordMethodClassification.WriteMethods;

    /// <summary>
    /// Subset of WriteMethods that trigger JIT loads when partial records are active.
    /// Per MS Docs: inserts, deletes, renames, field transfers, or copies to temporary records.
    /// Modify is excluded because it only writes changed fields without needing all fields loaded.
    /// Excludes ModifyAll (set-based, no record load), DeleteAll (same), Init (no SQL).
    /// </summary>
    private static readonly ImmutableHashSet<string> JitLoadWriteMethods = RecordMethodClassification.JitLoadWriteMethods;


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.UsePartialRecordsOnRead,
            DiagnosticDescriptors.PartialRecordsCauseJitLoad);

    // SetLoadFields was introduced in runtime 6.0 (BC17, Fall 2020).
    // Fall2020OrGreater would be ideal but is not available it seems
    // Spring2021OrGreater (BC18) is the closest safe version guard available.
    public override VersionCompatibility SupportedVersions =>
        VersionProvider.VersionCompatibility.Spring2021OrGreater;

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCodeBlockAction(this.AnalyzeCodeBlock);

    private void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
    {
        if (context.IsObsolete() ||
            context.CodeBlock is not MethodOrTriggerDeclarationSyntax methodOrTrigger)
            return;

        if (context.OwningSymbol is not IMethodSymbol methodSymbol)
            return;

        var body = methodOrTrigger.Body;
        if (body is null)
            return;

        var trackedVariables = CollectEligibleLocalVariables(methodSymbol);
        if (trackedVariables.Count == 0)
            return;

        var operation = context.SemanticModel.GetOperation(body, context.CancellationToken);
        if (operation is null)
            return;

        var walker = new SetLoadFieldsWalker(trackedVariables, context.CancellationToken);
        walker.Visit(operation);
        walker.FinalizeResults();

        foreach (var kvp in trackedVariables)
        {
            var state = kvp.Value;

            if (state.UncoveredReadLocations.Count == 0)
                continue;

            if (state.IsRecordRef && ShouldSuppressViaSetTable(state, trackedVariables))
                continue;

            foreach (var readInfo in state.UncoveredReadLocations)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UsePartialRecordsOnRead,
                    readInfo.Location,
                    kvp.Key, readInfo.MethodName));
            }
        }

        // PC0031: SetLoadFields used on a variable that also has full-field operations
        foreach (var kvp in trackedVariables)
        {
            var state = kvp.Value;

            if (state.LoadFieldsLocations.Count == 0)
                continue;

            var jitOperations = state.WriteMethodNames
                .Where(m => JitLoadWriteMethods.Contains(m))
                .OrderBy(m => m, SemanticFacts.NameComparer)
                .ToList();

            if (jitOperations.Count == 0)
                continue;

            var operationsText = string.Join(", ", jitOperations);

            foreach (var info in state.LoadFieldsLocations)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.PartialRecordsCauseJitLoad,
                    info.Location,
                    info.MethodName, operationsText, kvp.Key));
            }
        }
    }

    private static Dictionary<string, VariableState> CollectEligibleLocalVariables(IMethodSymbol methodSymbol)
    {
        var result = new Dictionary<string, VariableState>(SemanticFacts.NameEqualityComparer);

        foreach (var local in methodSymbol.LocalVariables)
        {
            if (IsEligibleVariable(local))
                result[local.Name] = new VariableState
                {
                    IsRecordRef = local.Type?.NavTypeKind == EnumProvider.NavTypeKind.RecordRef,
                    IsSetupTable = local.Type is IRecordTypeSymbol recordType
                        && recordType.OriginalDefinition is ITableTypeSymbol tableType
                        && TableHelper.IsSetupTable(tableType)
                };
        }

        return result;
    }

    /// <summary>
    /// When a RecordRef calls SetTable(TargetRecord), the RecordRef's diagnostic is suppressed if:
    /// - Any target is unresolvable (null) or not a tracked local variable (conservative)
    /// - Any tracked target ever had a suppression condition (write op, passed to function, load fields call)
    /// </summary>
    private static bool ShouldSuppressViaSetTable(VariableState state,
        Dictionary<string, VariableState> trackedVariables)
    {
        if (state.SetTableTargets.Count == 0)
            return false;

        foreach (var targetName in state.SetTableTargets)
        {
            if (targetName is null || !trackedVariables.TryGetValue(targetName, out var targetState))
                return true;

            if (targetState.EverHadFullRecordAccess || targetState.EverPassedToFunction || targetState.EverHadLoadFields)
                return true;
        }

        return false;
    }

    private static bool IsEligibleVariable(IVariableSymbol variable)
    {
        var type = variable.Type;
        if (type is null)
            return false;

        if (type is IRecordTypeSymbol recordType)
        {
            if (recordType.Temporary)
                return false;

            if (recordType.OriginalDefinition is ITableTypeSymbol tableType
                && tableType.TableType != EnumProvider.TableTypeKind.Normal)
                return false;

            return true;
        }

        return type.NavTypeKind == EnumProvider.NavTypeKind.RecordRef;
    }

    private sealed class VariableState
    {
        public List<ReadInfo> UncoveredReadLocations { get; } = new();
        public List<LoadFieldsInfo> LoadFieldsLocations { get; } = new();
        public HashSet<string> WriteMethodNames { get; } = new(SemanticFacts.NameEqualityComparer);
        public bool IsRecordRef { get; set; }
        public bool IsSetupTable { get; set; }
        public List<string?> SetTableTargets { get; } = new();

        public bool EverHadLoadFields { get; set; }
        public bool EverHadFullRecordAccess { get; set; }
        public bool EverPassedToFunction { get; set; }
    }

    /// <summary>
    /// Per-variable flow state tracked during the walk. Forked at branch points, merged at join points.
    /// UncoveredReads accumulates reads not covered by SetLoadFields. Full-record-access operations
    /// (writes, whole-record assignments) and pass-to-function operations retroactively clear this list
    /// (adding SetLoadFields before the read could break the subsequent operation).
    /// At branch merge, uncovered reads from all branches are unioned.
    /// </summary>
    private sealed class FlowFlags
    {
        public bool HasLoadFields { get; set; }
        public bool HasFullRecordAccess { get; set; }
        public bool PassedToFunction { get; set; }
        public bool HasPartialRead { get; set; }
        public List<ReadInfo> UncoveredReads { get; private set; } = new();
        public List<LoadFieldsInfo> LoadFieldsLocations { get; private set; } = new();
        public HashSet<string>? WriteMethodNamesAfterPartialRead { get; set; }

        public FlowFlags Clone() => new()
        {
            HasLoadFields = HasLoadFields,
            HasFullRecordAccess = HasFullRecordAccess,
            PassedToFunction = PassedToFunction,
            HasPartialRead = HasPartialRead,
            UncoveredReads = new List<ReadInfo>(UncoveredReads),
            LoadFieldsLocations = new List<LoadFieldsInfo>(LoadFieldsLocations),
            WriteMethodNamesAfterPartialRead = WriteMethodNamesAfterPartialRead is not null
                ? new HashSet<string>(WriteMethodNamesAfterPartialRead, SemanticFacts.NameEqualityComparer)
                : null
        };

        /// <summary>
        /// Resets boolean flow flags and LoadFieldsLocations (used by Clear/Reset operations).
        /// Does NOT clear UncoveredReads: reads before Clear are still genuinely uncovered.
        /// </summary>
        public void ResetFlags()
        {
            HasLoadFields = false;
            HasFullRecordAccess = false;
            PassedToFunction = false;
            HasPartialRead = false;
            LoadFieldsLocations.Clear();
            WriteMethodNamesAfterPartialRead = null;
        }

        public void AddWriteAfterPartialRead(string methodName)
        {
            WriteMethodNamesAfterPartialRead ??= new(SemanticFacts.NameEqualityComparer);
            WriteMethodNamesAfterPartialRead.Add(methodName);
        }
    }

#if NETSTANDARD2_1
    private readonly struct ReadInfo
    {
        public Location Location { get; }
        public string MethodName { get; }

        public ReadInfo(Location location, string methodName)
        {
            Location = location;
            MethodName = methodName;
        }
    }

    private readonly struct LoadFieldsInfo
    {
        public Location Location { get; }
        public string MethodName { get; }

        public LoadFieldsInfo(Location location, string methodName)
        {
            Location = location;
            MethodName = methodName;
        }
    }
#else
    private readonly record struct ReadInfo(Location Location, string MethodName);
    private readonly record struct LoadFieldsInfo(Location Location, string MethodName);
#endif

    private sealed class SetLoadFieldsWalker : OperationWalker
    {
        private readonly Dictionary<string, VariableState> _trackedVariables;
        private readonly Dictionary<string, FlowFlags> _flowState;
        private readonly CancellationToken _cancellationToken;

        public SetLoadFieldsWalker(Dictionary<string, VariableState> trackedVariables,
            CancellationToken cancellationToken)
        {
            _trackedVariables = trackedVariables;
            _cancellationToken = cancellationToken;
            _flowState = new Dictionary<string, FlowFlags>(SemanticFacts.NameEqualityComparer);
            foreach (var key in trackedVariables.Keys)
                _flowState[key] = new FlowFlags();
        }

        /// <summary>
        /// Copies final uncovered reads from flow state into VariableState for reporting.
        /// Deduplicates by source span start position (reads before a branch fork appear
        /// in both branch copies and get unioned at merge).
        /// </summary>
        public void FinalizeResults()
        {
            foreach (var kvp in _flowState)
            {
                if (!_trackedVariables.TryGetValue(kvp.Key, out var state))
                    continue;

                var seen = new HashSet<int>();
                foreach (var read in kvp.Value.UncoveredReads)
                {
                    if (seen.Add(read.Location.SourceSpan.Start))
                        state.UncoveredReadLocations.Add(read);
                }

                var seenLF = new HashSet<int>();
                foreach (var info in kvp.Value.LoadFieldsLocations)
                {
                    if (seenLF.Add(info.Location.SourceSpan.Start))
                        state.LoadFieldsLocations.Add(info);
                }

                if (kvp.Value.WriteMethodNamesAfterPartialRead is not null)
                    state.WriteMethodNames.UnionWith(kvp.Value.WriteMethodNamesAfterPartialRead);
            }
        }

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var instanceSymbol = operation.Instance?.GetSymbolSafe();

            if (instanceSymbol != null &&
                _trackedVariables.TryGetValue(instanceSymbol.Name, out var state) &&
                _flowState.TryGetValue(instanceSymbol.Name, out var flowFlags))
            {
                ClassifyInstanceMethodCall(operation, state, flowFlags);
            }

            CheckArgumentsForTrackedVariables(operation);

            base.VisitInvocationExpression(operation);
        }

        public override void VisitAssignmentStatement(IAssignmentStatement operation)
        {
            // Detect whole-record assignment where a tracked variable is the source (RHS):
            // TempMyTable := MyTable
            // This reads all fields from MyTable, so adding SetLoadFields before a
            // preceding read would cause only partial fields to be copied.
            var sourceVarName = GetVariableNameFromOperation(operation.Value);
            if (sourceVarName != null &&
                _trackedVariables.TryGetValue(sourceVarName, out var state) &&
                _flowState.TryGetValue(sourceVarName, out var flowFlags))
            {
                flowFlags.UncoveredReads.Clear();
                flowFlags.HasFullRecordAccess = true;
                state.EverHadFullRecordAccess = true;
            }

            base.VisitAssignmentStatement(operation);
        }

        #region Control flow overrides

        /// <summary>
        /// Tracks nesting depth inside conditional branches (if/case/while/for/foreach bodies).
        /// When depth > 0, JIT-load writes are suppressed for PC0031 because the write
        /// may not execute on the same path as the partial read (avoiding false positives).
        /// Repeat-until body does NOT increment depth (it always executes at least once).
        /// </summary>
        private int _branchDepth;

        public override void VisitIfStatement(IIfStatement operation)
        {
            Visit(operation.Condition);

            var preForkReads = CapturePreForkReadPositions();
            var preBranchState = SaveFlowState();

            _branchDepth++;
            Visit(operation.IfTrueStatement);
            _branchDepth--;
            var trueBranchState = SaveFlowState();

            RestoreFlowState(preBranchState);

            _branchDepth++;
            Visit(operation.IfFalseStatement);
            _branchDepth--;
            var falseBranchState = SaveFlowState();

            MergeFlowStates([trueBranchState, falseBranchState], preForkReads);
        }

        public override void VisitCaseStatement(ICaseStatement operation)
        {
            Visit(operation.Value);

            var preForkReads = CapturePreForkReadPositions();
            var preCaseState = SaveFlowState();
            var branchStates = new List<Dictionary<string, FlowFlags>>();

            foreach (var caseLine in operation.CaseLines)
            {
                RestoreFlowState(preCaseState);
                _branchDepth++;
                Visit(caseLine);
                _branchDepth--;
                branchStates.Add(SaveFlowState());
            }

            if (operation.ElseStatement != null)
            {
                RestoreFlowState(preCaseState);
                _branchDepth++;
                Visit(operation.ElseStatement);
                _branchDepth--;
                branchStates.Add(SaveFlowState());
            }
            else
            {
                // No else clause: implicit empty branch with pre-case state.
                // preCaseState is already a clone from SaveFlowState() and is not
                // restored again after this point, so it's safe to use directly.
                branchStates.Add(preCaseState);
            }

            MergeFlowStates(branchStates, preForkReads);
        }

        public override void VisitWhileRepeatLoopStatement(IWhileRepeatLoopStatement operation)
        {
            if (operation.LoopKind == EnumProvider.LoopKind.Repeat)
            {
                // repeat-until: body always executes at least once (no branch depth increment)
                Visit(operation.Body);
                Visit(operation.Condition);
            }
            else
            {
                // while-do: body might not execute
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
        /// Shared logic for loop bodies that may not execute (while-do, for, foreach).
        /// Forks state, visits body at incremented branch depth, then merges.
        /// </summary>
        private void VisitConditionalLoopBody(IOperation loopBody)
        {
            var preForkReads = CapturePreForkReadPositions();
            var preLoopState = SaveFlowState();
            _branchDepth++;
            Visit(loopBody);
            _branchDepth--;
            var postBodyState = SaveFlowState();

            MergeFlowStates([preLoopState, postBodyState], preForkReads);
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

        /// <summary>
        /// Captures current UncoveredReads positions per variable before a fork.
        /// Used during merge to apply intersection semantics for pre-fork reads.
        /// </summary>
        private Dictionary<string, HashSet<int>> CapturePreForkReadPositions()
        {
            var result = new Dictionary<string, HashSet<int>>(SemanticFacts.NameEqualityComparer);
            foreach (var kvp in _flowState)
            {
                if (kvp.Value.UncoveredReads.Count > 0)
                    result[kvp.Key] = new HashSet<int>(kvp.Value.UncoveredReads.Select(r => r.Location.SourceSpan.Start));
            }
            return result;
        }

        private void MergeFlowStates(List<Dictionary<string, FlowFlags>> branchStates,
            Dictionary<string, HashSet<int>>? preForkReads = null)
        {
            if (branchStates.Count == 0)
                return;

            foreach (var kvp in _flowState)
            {
                kvp.Value.HasLoadFields = branchStates.All(bs => bs[kvp.Key].HasLoadFields);
                kvp.Value.HasFullRecordAccess = branchStates.Any(bs => bs[kvp.Key].HasFullRecordAccess);
                kvp.Value.PassedToFunction = branchStates.Any(bs => bs[kvp.Key].PassedToFunction);
                kvp.Value.HasPartialRead = branchStates.Any(bs => bs[kvp.Key].HasPartialRead);

                HashSet<int>? preFork = null;
                preForkReads?.TryGetValue(kvp.Key, out preFork);

                kvp.Value.UncoveredReads.Clear();
                var seenReads = new HashSet<int>();
                foreach (var bs in branchStates)
                {
                    foreach (var read in bs[kvp.Key].UncoveredReads)
                    {
                        if (seenReads.Add(read.Location.SourceSpan.Start))
                            kvp.Value.UncoveredReads.Add(read);
                    }
                }

                // Pre-fork reads use intersection: keep only if present in ALL branches
                if (preFork is { Count: > 0 })
                {
                    kvp.Value.UncoveredReads.RemoveAll(read =>
                        preFork.Contains(read.Location.SourceSpan.Start) &&
                        !branchStates.All(bs => bs[kvp.Key].UncoveredReads.Any(
                            r => r.Location.SourceSpan.Start == read.Location.SourceSpan.Start)));
                }

                kvp.Value.LoadFieldsLocations.Clear();
                var seenLF = new HashSet<int>();
                foreach (var bs in branchStates)
                    foreach (var info in bs[kvp.Key].LoadFieldsLocations)
                        if (seenLF.Add(info.Location.SourceSpan.Start))
                            kvp.Value.LoadFieldsLocations.Add(info);

                MergeWriteMethodNames(kvp.Value,
                    branchStates.Select(bs => bs[kvp.Key].WriteMethodNamesAfterPartialRead).ToArray());
            }
        }

        private static void MergeWriteMethodNames(FlowFlags target, params HashSet<string>?[] sets)
        {
            HashSet<string>? merged = null;
            foreach (var set in sets)
            {
                if (set is null) continue;
                merged ??= new HashSet<string>(SemanticFacts.NameEqualityComparer);
                merged.UnionWith(set);
            }
            target.WriteMethodNamesAfterPartialRead = merged;
        }

        #endregion

        #region Method classification

        private void ClassifyInstanceMethodCall(IInvocationExpression operation,
            VariableState state, FlowFlags flowFlags)
        {
            var methodName = operation.TargetMethod.Name;

            if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            {
                // Non-built-in method called on the record variable (table-defined method):
                // retroactively suppress uncovered reads (callee might access any field)
                flowFlags.UncoveredReads.Clear();
                flowFlags.PassedToFunction = true;
                state.EverPassedToFunction = true;
                return;
            }

            if (ReadMethods.Contains(methodName))
                HandleReadMethod(operation, state, flowFlags, methodName);
            else if (LoadFieldsMethods.Contains(methodName))
                HandleLoadFieldsMethod(operation, state, flowFlags, methodName);
            else if (WriteMethods.Contains(methodName))
                HandleWriteMethod(state, flowFlags, methodName);
            else if (SemanticFacts.IsSameName(methodName, "Reset"))
                flowFlags.ResetFlags();
            else if (state.IsRecordRef
                && SemanticFacts.IsSameName(methodName, "SetTable")
                && operation.Arguments.Length == 1)
                state.SetTableTargets.Add(GetVariableNameFromArgument(operation.Arguments[0]));
        }

        private static void HandleReadMethod(IInvocationExpression operation, VariableState state, FlowFlags flowFlags, string methodName)
        {
            // PC0031: a read while HasLoadFields means the record buffer is now partial
            if (flowFlags.HasLoadFields)
                flowFlags.HasPartialRead = true;

            // Suppress on setup table parameterless Get() - near-zero performance benefit
            if (state.IsSetupTable
                && SemanticFacts.IsSameName(methodName, "Get")
                && operation.Arguments.IsEmpty)
                return;

            // Only flag reads where no suppression condition exists.
            // Write/pass AFTER read is handled by retroactive clearing of UncoveredReads.
            if (!flowFlags.HasLoadFields && !flowFlags.HasFullRecordAccess && !flowFlags.PassedToFunction)
                flowFlags.UncoveredReads.Add(new ReadInfo(operation.Syntax.GetLocation(), methodName));
        }

        private static void HandleLoadFieldsMethod(IInvocationExpression operation,
            VariableState state, FlowFlags flowFlags, string methodName)
        {
            if (SemanticFacts.IsSameName(methodName, "SetLoadFields")
                && operation.Arguments.IsEmpty)
            {
                // SetLoadFields() with no arguments resets partial records to "load all"
                flowFlags.HasLoadFields = false;
                flowFlags.HasPartialRead = false;
                flowFlags.LoadFieldsLocations.Clear();
                flowFlags.WriteMethodNamesAfterPartialRead = null;
                return;
            }

            flowFlags.HasLoadFields = true;
            flowFlags.LoadFieldsLocations.Add(
                new LoadFieldsInfo(operation.Syntax.GetLocation(), methodName));
            state.EverHadLoadFields = true;
        }

        private void HandleWriteMethod(VariableState state, FlowFlags flowFlags, string methodName)
        {
            // Retroactively suppress uncovered reads: a write after a read means
            // adding SetLoadFields before the read could break the write operation
            flowFlags.UncoveredReads.Clear();
            flowFlags.HasFullRecordAccess = true;
            state.EverHadFullRecordAccess = true;

            // PC0031: only record writes for JIT-load detection when:
            // 1. A partial read has occurred on this flow path
            // 2. The method is a JIT-load trigger (Insert, Delete, Rename, TransferFields, Copy)
            // 3. The write is at the top level (not inside a conditional branch)
            //    This avoids false positives where the write only executes on a path
            //    where the record wasn't actually loaded (e.g., if not Find then Insert).
            if (flowFlags.HasPartialRead && JitLoadWriteMethods.Contains(methodName) && _branchDepth == 0)
                flowFlags.AddWriteAfterPartialRead(methodName);
        }

        private void CheckArgumentsForTrackedVariables(IInvocationExpression operation)
        {
            // Clear(variable) resets flow state instead of marking passedToFunction
            if (operation.TargetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod
                && SemanticFacts.IsSameName(operation.TargetMethod.Name, "Clear")
                && operation.Arguments.Length >= 1)
            {
                var clearVarName = GetVariableNameFromArgument(operation.Arguments[0]);
                if (clearVarName != null && _flowState.TryGetValue(clearVarName, out var clearFlags))
                {
                    clearFlags.ResetFlags();
                    return;
                }
            }

            for (int i = 0; i < operation.Arguments.Length; i++)
            {
                var varName = GetVariableNameFromArgument(operation.Arguments[i]);
                if (varName != null && _flowState.TryGetValue(varName, out var flags))
                {
                    // Retroactively suppress uncovered reads: callee might access any field
                    flags.UncoveredReads.Clear();
                    flags.PassedToFunction = true;
                    if (_trackedVariables.TryGetValue(varName, out var state))
                        state.EverPassedToFunction = true;
                }
            }
        }

        #endregion

        private static string? GetVariableNameFromArgument(IArgument argument) =>
            GetVariableNameFromOperation(argument.Value);

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
