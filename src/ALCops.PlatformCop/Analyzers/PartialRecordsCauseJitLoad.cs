using System.Collections.Immutable;
using ALCops.Common;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PartialRecordsCauseJitLoad : DiagnosticAnalyzer
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
        ImmutableArray.Create(DiagnosticDescriptors.PartialRecordsCauseJitLoad);

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

        // SetLoadFields used on a variable that also has full-field operations
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
                result[local.Name] = new VariableState();
        }

        return result;
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
        public List<LoadFieldsInfo> LoadFieldsLocations { get; } = new();
        public HashSet<string> WriteMethodNames { get; } = new(SemanticFacts.NameEqualityComparer);
    }

    /// <summary>
    /// Per-variable flow state tracked during the walk. Forked at branch points, merged at join points.
    /// </summary>
    private sealed class FlowFlags
    {
        public bool HasLoadFields { get; set; }
        public bool HasPartialRead { get; set; }
        public List<LoadFieldsInfo> LoadFieldsLocations { get; private set; } = new();
        public HashSet<string>? WriteMethodNamesAfterPartialRead { get; set; }

        public FlowFlags Clone() => new()
        {
            HasLoadFields = HasLoadFields,
            HasPartialRead = HasPartialRead,
            LoadFieldsLocations = new List<LoadFieldsInfo>(LoadFieldsLocations),
            WriteMethodNamesAfterPartialRead = WriteMethodNamesAfterPartialRead is not null
                ? new HashSet<string>(WriteMethodNamesAfterPartialRead, SemanticFacts.NameEqualityComparer)
                : null
        };

        /// <summary>
        /// Resets the flow flags and LoadFieldsLocations (used by Clear/Reset operations).
        /// </summary>
        public void ResetFlags()
        {
            HasLoadFields = false;
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
        /// Copies the final flow state into VariableState for reporting.
        /// Deduplicates by source span start position (locations before a branch fork appear
        /// in both branch copies and get unioned at merge).
        /// </summary>
        public void FinalizeResults()
        {
            foreach (var kvp in _flowState)
            {
                if (!_trackedVariables.TryGetValue(kvp.Key, out var state))
                    continue;

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

            // The map holds locals only; `this.<Global>` or `this.<DataItem>` binds to
            // another symbol that may share the name.
            if (instanceSymbol?.Kind == EnumProvider.SymbolKind.LocalVariable &&
                _flowState.TryGetValue(instanceSymbol.Name, out var flowFlags))
            {
                ClassifyInstanceMethodCall(operation, flowFlags);
            }

            CheckClearOfTrackedVariable(operation);

            base.VisitInvocationExpression(operation);
        }

        #region Control flow overrides

        /// <summary>
        /// Tracks nesting depth inside conditional branches (if/case/while/for/foreach bodies).
        /// When depth > 0, JIT-load writes are suppressed because the write may not execute on
        /// the same path as the partial read (avoiding false positives).
        /// Repeat-until body does NOT increment depth (it always executes at least once).
        /// </summary>
        private int _branchDepth;

        public override void VisitIfStatement(IIfStatement operation)
        {
            Visit(operation.Condition);

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

            MergeFlowStates([trueBranchState, falseBranchState]);
        }

        public override void VisitCaseStatement(ICaseStatement operation)
        {
            Visit(operation.Value);

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

            MergeFlowStates(branchStates);
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
            var preLoopState = SaveFlowState();
            _branchDepth++;
            Visit(loopBody);
            _branchDepth--;
            var postBodyState = SaveFlowState();

            MergeFlowStates([preLoopState, postBodyState]);
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

        private void MergeFlowStates(List<Dictionary<string, FlowFlags>> branchStates)
        {
            if (branchStates.Count == 0)
                return;

            foreach (var kvp in _flowState)
            {
                kvp.Value.HasLoadFields = branchStates.All(bs => bs[kvp.Key].HasLoadFields);
                kvp.Value.HasPartialRead = branchStates.Any(bs => bs[kvp.Key].HasPartialRead);

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

        private void ClassifyInstanceMethodCall(IInvocationExpression operation, FlowFlags flowFlags)
        {
            var methodName = operation.TargetMethod.Name;

            if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
                return;

            if (ReadMethods.Contains(methodName))
                HandleReadMethod(flowFlags);
            else if (LoadFieldsMethods.Contains(methodName))
                HandleLoadFieldsMethod(operation, flowFlags, methodName);
            else if (WriteMethods.Contains(methodName))
                HandleWriteMethod(flowFlags, methodName);
            else if (SemanticFacts.IsSameName(methodName, "Reset"))
                flowFlags.ResetFlags();
        }

        private static void HandleReadMethod(FlowFlags flowFlags)
        {
            // A read while HasLoadFields means the record buffer is now partial
            if (flowFlags.HasLoadFields)
                flowFlags.HasPartialRead = true;
        }

        private static void HandleLoadFieldsMethod(IInvocationExpression operation,
            FlowFlags flowFlags, string methodName)
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
        }

        private void HandleWriteMethod(FlowFlags flowFlags, string methodName)
        {
            // Only record writes for JIT-load detection when:
            // 1. A partial read has occurred on this flow path
            // 2. The method is a JIT-load trigger (Insert, Delete, Rename, TransferFields, Copy)
            // 3. The write is at the top level (not inside a conditional branch)
            //    This avoids false positives where the write only executes on a path
            //    where the record wasn't actually loaded (e.g., if not Find then Insert).
            if (flowFlags.HasPartialRead && JitLoadWriteMethods.Contains(methodName) && _branchDepth == 0)
                flowFlags.AddWriteAfterPartialRead(methodName);
        }

        private void CheckClearOfTrackedVariable(IInvocationExpression operation)
        {
            // Clear(variable) resets the flow state
            if (operation.TargetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod
                && SemanticFacts.IsSameName(operation.TargetMethod.Name, "Clear")
                && operation.Arguments.Length >= 1)
            {
                var clearVarName = GetVariableNameFromOperation(operation.Arguments[0].Value);
                if (clearVarName != null && _flowState.TryGetValue(clearVarName, out var clearFlags))
                    clearFlags.ResetFlags();
            }
        }

        #endregion

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
