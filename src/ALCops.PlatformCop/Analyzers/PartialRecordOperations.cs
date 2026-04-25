using System.Collections.Immutable;
using ALCops.Common;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PartialRecordOperations : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> ReadMethods = RecordMethodClassification.SingleRecordReadMethods
        .Union(ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "FindSet"));

    private static readonly ImmutableHashSet<string> LoadFieldsMethods = RecordMethodClassification.LoadFieldsMethods;

    private static readonly ImmutableHashSet<string> WriteMethods = RecordMethodClassification.WriteMethods;

    /// <summary>
    /// Subset of WriteMethods that trigger JIT loads when partial records are active.
    /// Excludes ModifyAll (set-based, no record load), DeleteAll (same), Init (no SQL).
    /// </summary>
    private static readonly ImmutableHashSet<string> JitLoadWriteMethods = RecordMethodClassification.JitLoadWriteMethods;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.UsePartialRecordsOnRead,
            DiagnosticDescriptors.PartialRecordsBeforeWriteOperation);

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

        // PC0031: SetLoadFields used on a variable that also has write operations
        foreach (var kvp in trackedVariables)
        {
            var state = kvp.Value;

            if (state.LoadFieldsLocations.Count == 0)
                continue;

            var jitWriteMethods = state.WriteMethodNames
                .Where(JitLoadWriteMethods.Contains)
                .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (jitWriteMethods.Count == 0)
                continue;

            var writeMethodsText = string.Join(", ", jitWriteMethods);

            foreach (var info in state.LoadFieldsLocations)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.PartialRecordsBeforeWriteOperation,
                    info.Location,
                    info.MethodName, writeMethodsText, kvp.Key));
            }
        }
    }

    private static Dictionary<string, VariableState> CollectEligibleLocalVariables(IMethodSymbol methodSymbol)
    {
        var result = new Dictionary<string, VariableState>(StringComparer.OrdinalIgnoreCase);

        foreach (var local in methodSymbol.LocalVariables)
        {
            if (IsEligibleVariable(local))
                result[local.Name] = new VariableState
                {
                    IsRecordRef = local.Type?.NavTypeKind == EnumProvider.NavTypeKind.RecordRef
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

            if (targetState.EverHadWriteOp || targetState.EverPassedToFunction || targetState.EverHadLoadFields)
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
        public HashSet<string> WriteMethodNames { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool IsRecordRef { get; set; }
        public List<string?> SetTableTargets { get; } = new();

        public bool EverHadLoadFields { get; set; }
        public bool EverHadWriteOp { get; set; }
        public bool EverPassedToFunction { get; set; }
    }

    /// <summary>
    /// Per-variable flow state tracked during the walk. Forked at branch points, merged at join points.
    /// UncoveredReads accumulates reads not covered by SetLoadFields. Write/pass operations
    /// retroactively clear this list (a write after a read means SetLoadFields could break the write).
    /// At branch merge, uncovered reads from all branches are unioned.
    /// </summary>
    private sealed class FlowFlags
    {
        public bool HasLoadFields { get; set; }
        public bool HasWriteOp { get; set; }
        public bool PassedToFunction { get; set; }
        public bool HasPartialRead { get; set; }
        public List<ReadInfo> UncoveredReads { get; private set; } = new();
        public List<LoadFieldsInfo> LoadFieldsLocations { get; private set; } = new();
        public HashSet<string>? WriteMethodNamesAfterPartialRead { get; set; }

        public FlowFlags Clone() => new()
        {
            HasLoadFields = HasLoadFields,
            HasWriteOp = HasWriteOp,
            PassedToFunction = PassedToFunction,
            HasPartialRead = HasPartialRead,
            UncoveredReads = new List<ReadInfo>(UncoveredReads),
            LoadFieldsLocations = new List<LoadFieldsInfo>(LoadFieldsLocations),
            WriteMethodNamesAfterPartialRead = WriteMethodNamesAfterPartialRead is not null
                ? new HashSet<string>(WriteMethodNamesAfterPartialRead, StringComparer.OrdinalIgnoreCase)
                : null
        };

        /// <summary>
        /// Resets boolean flow flags and LoadFieldsLocations (used by Clear/Reset operations).
        /// Does NOT clear UncoveredReads: reads before Clear are still genuinely uncovered.
        /// </summary>
        public void ResetFlags()
        {
            HasLoadFields = false;
            HasWriteOp = false;
            PassedToFunction = false;
            HasPartialRead = false;
            LoadFieldsLocations.Clear();
            WriteMethodNamesAfterPartialRead = null;
        }

        public void AddWriteAfterPartialRead(string methodName)
        {
            WriteMethodNamesAfterPartialRead ??= new(StringComparer.OrdinalIgnoreCase);
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
            _flowState = new Dictionary<string, FlowFlags>(StringComparer.OrdinalIgnoreCase);
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

        #region Control flow overrides

        public override void VisitIfStatement(IIfStatement operation)
        {
            Visit(operation.Condition);

            var preBranchState = SaveFlowState();

            Visit(operation.IfTrueStatement);
            var trueBranchState = SaveFlowState();

            RestoreFlowState(preBranchState);
            Visit(operation.IfFalseStatement);
            var falseBranchState = SaveFlowState();

            MergeFlowStates(trueBranchState, falseBranchState);
        }

        public override void VisitCaseStatement(ICaseStatement operation)
        {
            Visit(operation.Value);

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
                // No else clause: implicit empty branch with pre-case state
                branchStates.Add(CloneState(preCaseState));
            }

            MergeFlowStates(branchStates);
        }

        public override void VisitWhileRepeatLoopStatement(IWhileRepeatLoopStatement operation)
        {
            if (operation.LoopKind == EnumProvider.LoopKind.Repeat)
            {
                // repeat-until: body always executes at least once
                Visit(operation.Body);
                Visit(operation.Condition);
            }
            else
            {
                // while-do: body might not execute
                Visit(operation.Condition);

                var preLoopState = SaveFlowState();
                Visit(operation.Body);
                var postBodyState = SaveFlowState();

                MergeFlowStates(preLoopState, postBodyState);
            }
        }

        public override void VisitForLoopStatement(IForLoopStatement operation)
        {
            Visit(operation.InitialValue);
            Visit(operation.EndValue);

            var preLoopState = SaveFlowState();
            Visit(operation.Body);
            var postBodyState = SaveFlowState();

            MergeFlowStates(preLoopState, postBodyState);
        }

        public override void VisitForEachLoopStatement(IForEachLoopStatement operation)
        {
            Visit(operation.Expression);

            var preLoopState = SaveFlowState();
            Visit(operation.Body);
            var postBodyState = SaveFlowState();

            MergeFlowStates(preLoopState, postBodyState);
        }

        #endregion

        #region Flow state management

        private Dictionary<string, FlowFlags> SaveFlowState()
        {
            var saved = new Dictionary<string, FlowFlags>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _flowState)
                saved[kvp.Key] = kvp.Value.Clone();
            return saved;
        }

        private void RestoreFlowState(Dictionary<string, FlowFlags> saved)
        {
            foreach (var kvp in saved)
            {
                if (_flowState.TryGetValue(kvp.Key, out var current))
                {
                    current.HasLoadFields = kvp.Value.HasLoadFields;
                    current.HasWriteOp = kvp.Value.HasWriteOp;
                    current.PassedToFunction = kvp.Value.PassedToFunction;
                    current.UncoveredReads.Clear();
                    current.UncoveredReads.AddRange(kvp.Value.UncoveredReads);
                }
            }
        }

        private static Dictionary<string, FlowFlags> CloneState(Dictionary<string, FlowFlags> source)
        {
            var clone = new Dictionary<string, FlowFlags>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in source)
                clone[kvp.Key] = kvp.Value.Clone();
            return clone;
        }

        private void MergeFlowStates(Dictionary<string, FlowFlags> stateA, Dictionary<string, FlowFlags> stateB)
        {
            foreach (var kvp in _flowState)
            {
                var a = stateA[kvp.Key];
                var b = stateB[kvp.Key];

                kvp.Value.HasLoadFields = a.HasLoadFields && b.HasLoadFields;
                kvp.Value.HasWriteOp = a.HasWriteOp || b.HasWriteOp;
                kvp.Value.PassedToFunction = a.PassedToFunction || b.PassedToFunction;
                kvp.Value.HasPartialRead = a.HasPartialRead || b.HasPartialRead;

                MergeUncoveredReads(kvp.Value, a.UncoveredReads, b.UncoveredReads);
                MergeLoadFieldsLocations(kvp.Value, a.LoadFieldsLocations, b.LoadFieldsLocations);
                MergeWriteMethodNames(kvp.Value, a.WriteMethodNamesAfterPartialRead, b.WriteMethodNamesAfterPartialRead);
            }
        }

        private void MergeFlowStates(List<Dictionary<string, FlowFlags>> branchStates)
        {
            if (branchStates.Count == 0)
                return;

            foreach (var kvp in _flowState)
            {
                kvp.Value.HasLoadFields = branchStates.All(bs => bs[kvp.Key].HasLoadFields);
                kvp.Value.HasWriteOp = branchStates.Any(bs => bs[kvp.Key].HasWriteOp);
                kvp.Value.PassedToFunction = branchStates.Any(bs => bs[kvp.Key].PassedToFunction);
                kvp.Value.HasPartialRead = branchStates.Any(bs => bs[kvp.Key].HasPartialRead);

                kvp.Value.UncoveredReads.Clear();
                var seenReads = new HashSet<int>();
                foreach (var bs in branchStates)
                    foreach (var read in bs[kvp.Key].UncoveredReads)
                        if (seenReads.Add(read.Location.SourceSpan.Start))
                            kvp.Value.UncoveredReads.Add(read);

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

        /// <summary>
        /// Merges uncovered reads from two branches with deduplication by source position.
        /// Pre-fork reads exist in both branches; without dedup, list size doubles at each
        /// nesting level causing exponential growth and OOM on large codebases.
        /// </summary>
        private static void MergeUncoveredReads(FlowFlags target,
            List<ReadInfo> readsA, List<ReadInfo> readsB)
        {
            target.UncoveredReads.Clear();

            var seen = new HashSet<int>();
            foreach (var read in readsA)
                if (seen.Add(read.Location.SourceSpan.Start))
                    target.UncoveredReads.Add(read);

            foreach (var read in readsB)
                if (seen.Add(read.Location.SourceSpan.Start))
                    target.UncoveredReads.Add(read);
        }

        private static void MergeLoadFieldsLocations(FlowFlags target,
            List<LoadFieldsInfo> locsA, List<LoadFieldsInfo> locsB)
        {
            target.LoadFieldsLocations.Clear();

            var seen = new HashSet<int>();
            foreach (var info in locsA)
                if (seen.Add(info.Location.SourceSpan.Start))
                    target.LoadFieldsLocations.Add(info);

            foreach (var info in locsB)
                if (seen.Add(info.Location.SourceSpan.Start))
                    target.LoadFieldsLocations.Add(info);
        }

        private static void MergeWriteMethodNames(FlowFlags target,
            HashSet<string>? setA, HashSet<string>? setB)
        {
            if (setA is null && setB is null)
            {
                target.WriteMethodNamesAfterPartialRead = null;
                return;
            }

            var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (setA is not null) merged.UnionWith(setA);
            if (setB is not null) merged.UnionWith(setB);
            target.WriteMethodNamesAfterPartialRead = merged.Count > 0 ? merged : null;
        }

        private static void MergeWriteMethodNames(FlowFlags target, params HashSet<string>?[] sets)
        {
            HashSet<string>? merged = null;
            foreach (var set in sets)
            {
                if (set is null) continue;
                merged ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                merged.UnionWith(set);
            }
            target.WriteMethodNamesAfterPartialRead = merged;
        }

        #endregion

        #region Method classification

        private static void ClassifyInstanceMethodCall(IInvocationExpression operation,
            VariableState state, FlowFlags flowFlags)
        {
            var methodName = operation.TargetMethod.Name;

            if (operation.TargetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod)
            {
                if (ReadMethods.Contains(methodName))
                {
                    // PC0031: a read while HasLoadFields means the record buffer is now partial
                    if (flowFlags.HasLoadFields)
                        flowFlags.HasPartialRead = true;

                    // Check ALL flow flags: hasLoadFields (SetLoadFields before read),
                    // hasWriteOp (write before read means SetLoadFields could break the write),
                    // passedToFunction (callee might need all fields).
                    // Write/pass AFTER read is handled by retroactive clearing of UncoveredReads.
                    if (!flowFlags.HasLoadFields && !flowFlags.HasWriteOp && !flowFlags.PassedToFunction)
                        flowFlags.UncoveredReads.Add(new ReadInfo(operation.Syntax.GetLocation(), methodName));
                }
                else if (LoadFieldsMethods.Contains(methodName))
                {
                    if (string.Equals(methodName, "SetLoadFields", StringComparison.OrdinalIgnoreCase)
                        && operation.Arguments.IsEmpty)
                    {
                        // SetLoadFields() with no arguments resets partial records to "load all"
                        flowFlags.HasLoadFields = false;
                        flowFlags.HasPartialRead = false;
                        flowFlags.LoadFieldsLocations.Clear();
                        flowFlags.WriteMethodNamesAfterPartialRead = null;
                    }
                    else
                    {
                        flowFlags.HasLoadFields = true;
                        flowFlags.LoadFieldsLocations.Add(
                            new LoadFieldsInfo(operation.Syntax.GetLocation(), methodName));
                        state.EverHadLoadFields = true;
                    }
                }
                else if (WriteMethods.Contains(methodName))
                {
                    // Retroactively suppress uncovered reads: a write after a read means
                    // adding SetLoadFields before the read could break the write operation
                    flowFlags.UncoveredReads.Clear();
                    flowFlags.HasWriteOp = true;
                    state.EverHadWriteOp = true;

                    // PC0031: only record writes for JIT-load detection when a partial read
                    // has occurred on this flow path. Writes before SetLoadFields or between
                    // SetLoadFields and the next read use the full record buffer and don't JIT.
                    if (flowFlags.HasPartialRead && JitLoadWriteMethods.Contains(methodName))
                        flowFlags.AddWriteAfterPartialRead(methodName);
                }
                else if (string.Equals(methodName, "Reset", StringComparison.OrdinalIgnoreCase))
                {
                    flowFlags.ResetFlags();
                }
                else if (state.IsRecordRef
                    && string.Equals(methodName, "SetTable", StringComparison.OrdinalIgnoreCase)
                    && operation.Arguments.Length == 1)
                    state.SetTableTargets.Add(GetVariableNameFromArgument(operation.Arguments[0]));
            }
            else
            {
                // Non-built-in method called on the record variable (table-defined method):
                // retroactively suppress uncovered reads (callee might access any field)
                flowFlags.UncoveredReads.Clear();
                flowFlags.PassedToFunction = true;
                state.EverPassedToFunction = true;
            }
        }

        private void CheckArgumentsForTrackedVariables(IInvocationExpression operation)
        {
            // Clear(variable) resets flow state instead of marking passedToFunction
            if (operation.TargetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod
                && string.Equals(operation.TargetMethod.Name, "Clear", StringComparison.OrdinalIgnoreCase)
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

        private static string? GetVariableNameFromArgument(IArgument argument)
        {
            var value = argument.Value;

            if (value is IConversionExpression conversion)
            {
                if (conversion.Syntax is IdentifierNameSyntax convIdent)
                    return convIdent.Identifier.ValueText;

                value = conversion.Operand;
            }

            if (value?.Syntax is IdentifierNameSyntax directIdent)
                return directIdent.Identifier.ValueText;

            return null;
        }
    }
}
