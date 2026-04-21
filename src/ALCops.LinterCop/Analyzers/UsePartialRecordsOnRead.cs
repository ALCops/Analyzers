using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class UsePartialRecordsOnRead : DiagnosticAnalyzer
{
    private static readonly HashSet<string> ReadMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Get", "Find", "FindFirst", "FindLast", "FindSet"
    };

    private static readonly HashSet<string> LoadFieldsMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "SetLoadFields", "AddLoadFields", "SetBaseLoadFields"
    };

    private static readonly HashSet<string> WriteMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Insert", "Modify", "ModifyAll", "Delete", "DeleteAll",
        "Rename", "TransferFields", "Init", "Copy"
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.UsePartialRecordsOnRead);

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

        foreach (var kvp in trackedVariables)
        {
            var state = kvp.Value;

            if (state.ReadLocations.Count == 0 ||
                state.HasLoadFieldsCall ||
                state.HasWriteOp ||
                state.PassedToFunction)
                continue;

            if (state.IsRecordRef && ShouldSuppressViaSetTable(state, trackedVariables))
                continue;

            foreach (var readInfo in state.ReadLocations)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UsePartialRecordsOnRead,
                    readInfo.Location,
                    kvp.Key, readInfo.MethodName));
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
    /// - Any tracked target has a suppression condition (write op, passed to function, has load fields call)
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

            if (targetState.HasWriteOp || targetState.PassedToFunction || targetState.HasLoadFieldsCall)
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
        public List<ReadInfo> ReadLocations { get; } = new();
        public bool HasLoadFieldsCall { get; set; }
        public bool HasWriteOp { get; set; }
        public bool PassedToFunction { get; set; }
        public bool IsRecordRef { get; set; }
        public List<string?> SetTableTargets { get; } = new();
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
#else
    private readonly record struct ReadInfo(Location Location, string MethodName);
#endif

    private sealed class SetLoadFieldsWalker : OperationWalker
    {
        private readonly Dictionary<string, VariableState> _trackedVariables;
        private readonly CancellationToken _cancellationToken;

        public SetLoadFieldsWalker(Dictionary<string, VariableState> trackedVariables,
            CancellationToken cancellationToken)
        {
            _trackedVariables = trackedVariables;
            _cancellationToken = cancellationToken;
        }

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var instanceSymbol = operation.Instance?.GetSymbolSafe();

            if (instanceSymbol != null &&
                _trackedVariables.TryGetValue(instanceSymbol.Name, out var state))
            {
                ClassifyInstanceMethodCall(operation, state);
            }

            CheckArgumentsForTrackedVariables(operation);

            base.VisitInvocationExpression(operation);
        }

        private static void ClassifyInstanceMethodCall(IInvocationExpression operation, VariableState state)
        {
            var methodName = operation.TargetMethod.Name;

            if (operation.TargetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod)
            {
                if (ReadMethods.Contains(methodName))
                    state.ReadLocations.Add(new ReadInfo(operation.Syntax.GetLocation(), methodName));
                else if (LoadFieldsMethods.Contains(methodName))
                    state.HasLoadFieldsCall = true;
                else if (WriteMethods.Contains(methodName))
                    state.HasWriteOp = true;
                else if (state.IsRecordRef
                    && string.Equals(methodName, "SetTable", StringComparison.OrdinalIgnoreCase)
                    && operation.Arguments.Length == 1)
                    state.SetTableTargets.Add(GetVariableNameFromArgument(operation.Arguments[0]));
            }
            else
            {
                // Non-built-in method called on the record variable (table-defined method)
                state.PassedToFunction = true;
            }
        }

        private void CheckArgumentsForTrackedVariables(IInvocationExpression operation)
        {
            for (int i = 0; i < operation.Arguments.Length; i++)
            {
                var varName = GetVariableNameFromArgument(operation.Arguments[i]);
                if (varName != null && _trackedVariables.TryGetValue(varName, out var state))
                    state.PassedToFunction = true;
            }
        }

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
