using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class UseSequentialGuid : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.UseSequentialGuid);

    public override VersionCompatibility SupportedVersions =>
        VersionProvider.VersionCompatibility.Fall2025OrGreater;

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCodeBlockAction(AnalyzeCodeBlock);

    private static void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
    {
        if (context.IsObsolete() ||
            context.CodeBlock is not MethodOrTriggerDeclarationSyntax methodOrTrigger)
            return;

        var body = methodOrTrigger.Body;
        if (body is null)
            return;

        var workspacePath = context.SemanticModel.Compilation.FileSystem?.GetDirectoryPath();
        var settings = ALCopsSettingsProvider.GetSettings(workspacePath);
        bool flagAllGuidFields = string.Equals(
            settings.UseSequentialGuidScope, "AllGuidFields", StringComparison.OrdinalIgnoreCase);

        var operation = context.SemanticModel.GetOperation(body, context.CancellationToken);
        if (operation is null)
            return;

        var walker = new CreateGuidFlowWalker(
            context, flagAllGuidFields, context.CancellationToken);
        walker.Visit(operation);
    }

    private static void ReportDiagnostic(
        CodeBlockAnalysisContext context, IInvocationExpression createGuidCall, string reason)
    {
        var syntax = createGuidCall.Syntax;

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UseSequentialGuid,
            syntax.GetLocation(),
            syntax.ToString(),
            reason));
    }

    #region Data types

#if NETSTANDARD2_1
    private readonly struct KeyFieldResult
    {
        public string FieldName { get; }
        public string TableName { get; }

        public KeyFieldResult(string fieldName, string tableName)
        {
            FieldName = fieldName;
            TableName = tableName;
        }
    }
#else
    private readonly record struct KeyFieldResult(string FieldName, string TableName);
#endif

    #endregion

    #region Single-pass flow walker

    /// <summary>
    /// Single-pass walker that visits assignments and invocations, checking if their
    /// value/argument is a CreateGuid() call that flows to a key field.
    /// </summary>
    private sealed class CreateGuidFlowWalker : OperationWalker
    {
        private readonly CodeBlockAnalysisContext _context;
        private readonly bool _flagAllGuidFields;
        private readonly CancellationToken _ct;

        public CreateGuidFlowWalker(
            CodeBlockAnalysisContext context, bool flagAllGuidFields,
            CancellationToken ct)
        {
            _context = context;
            _flagAllGuidFields = flagAllGuidFields;
            _ct = ct;
        }

        public override void VisitAssignmentStatement(IAssignmentStatement operation)
        {
            _ct.ThrowIfCancellationRequested();

            var value = UnwrapConversion(operation.Value);
            if (IsCreateGuidCall(value, out var createGuidInvocation))
            {
                if (_flagAllGuidFields)
                {
                    ReportDiagnostic(_context, createGuidInvocation,
                        "Sequential GUIDs improve index performance.");
                }
                else if (operation.Target is IFieldAccess fieldAccess)
                {
                    var result = CheckFieldInKey(fieldAccess);
                    if (result is not null)
                    {
                        ReportDiagnostic(_context, createGuidInvocation,
                            $"The value flows to key field '{result.Value.FieldName}' in table '{result.Value.TableName}'.");
                    }
                }
                else
                {
                    // Variable assignment: v := CreateGuid()
                    var targetSymbol = operation.Target?.GetSymbolSafe();
                    if (targetSymbol is not null && targetSymbol.Kind == EnumProvider.SymbolKind.LocalVariable)
                    {
                        var bodyOp = _context.SemanticModel.GetOperation(
                            ((MethodOrTriggerDeclarationSyntax)_context.CodeBlock).Body!,
                            _ct);
                        if (bodyOp is not null)
                        {
                            var result = TraceVariable(
                                targetSymbol, bodyOp, _context.SemanticModel.Compilation, _ct,
                                new HashSet<IMethodSymbol>());
                            if (result is not null)
                            {
                                ReportDiagnostic(_context, createGuidInvocation,
                                    $"The value flows to key field '{result.Value.FieldName}' in table '{result.Value.TableName}'.");
                            }
                        }
                    }
                }
            }

            base.VisitAssignmentStatement(operation);
        }

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            _ct.ThrowIfCancellationRequested();

            for (int i = 0; i < operation.Arguments.Length; i++)
            {
                var argValue = UnwrapConversion(operation.Arguments[i].Value);
                if (!IsCreateGuidCall(argValue, out var createGuidInvocation))
                    continue;

                if (_flagAllGuidFields)
                {
                    ReportDiagnostic(_context, createGuidInvocation,
                        "Sequential GUIDs improve index performance.");
                    continue;
                }

                // Validate(Field, CreateGuid())
                if (IsValidateCall(operation) && i == 1 && operation.Arguments.Length >= 2)
                {
                    var result = CheckValidateTarget(operation);
                    if (result is not null)
                    {
                        ReportDiagnostic(_context, createGuidInvocation,
                            $"The value flows to key field '{result.Value.FieldName}' in table '{result.Value.TableName}'.");
                    }
                    continue;
                }

                // User procedure argument (skip events and built-in methods)
                if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod &&
                    !operation.TargetMethod.IsEvent)
                {
                    var result = TraceParameter(
                        operation.TargetMethod, i, _context.SemanticModel.Compilation, _ct,
                        new HashSet<IMethodSymbol>());
                    if (result is not null)
                    {
                        ReportDiagnostic(_context, createGuidInvocation,
                            $"The value flows to key field '{result.Value.FieldName}' in table '{result.Value.TableName}'.");
                    }
                }
            }

            base.VisitInvocationExpression(operation);
        }

        private static bool IsCreateGuidCall(
            IOperation operation, out IInvocationExpression invocation)
        {
            if (operation is IInvocationExpression inv &&
                inv.TargetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod &&
                string.Equals(inv.TargetMethod.Name, "CreateGuid", StringComparison.OrdinalIgnoreCase) &&
                inv.Arguments.IsEmpty)
            {
                invocation = inv;
                return true;
            }

            invocation = null!;
            return false;
        }
    }

    #endregion

    #region Cross-procedure tracing

    private static KeyFieldResult? TraceVariable(
        ISymbol variable, IOperation methodBody, Compilation compilation,
        CancellationToken ct, HashSet<IMethodSymbol> visited)
    {
        var tracer = new SymbolFlowTracer(variable, compilation, ct, visited);
        tracer.Visit(methodBody);
        return tracer.Result;
    }

    private static KeyFieldResult? TraceParameter(
        IMethodSymbol method, int paramIndex, Compilation compilation,
        CancellationToken ct, HashSet<IMethodSymbol> visited)
    {
        ct.ThrowIfCancellationRequested();

        if (paramIndex >= method.Parameters.Length)
            return null;

        var syntaxRef = method.DeclaringSyntaxReference;
        if (syntaxRef is null)
            return null;

        if (!visited.Add(method))
            return null;

        try
        {
            var syntax = syntaxRef.GetSyntax(ct);
            if (syntax is not MethodOrTriggerDeclarationSyntax methodSyntax || methodSyntax.Body is null)
                return null;

            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
            var bodyOp = semanticModel.GetOperation(methodSyntax.Body, ct);
            if (bodyOp is null)
                return null;

            var parameter = method.Parameters[paramIndex];
            var tracer = new SymbolFlowTracer(parameter, compilation, ct, visited);
            tracer.Visit(bodyOp);
            return tracer.Result;
        }
        finally
        {
            visited.Remove(method);
        }
    }

    #endregion

    #region SymbolFlowTracer

    private sealed class SymbolFlowTracer : OperationWalker
    {
        private readonly ISymbol _tracked;
        private readonly Compilation _compilation;
        private readonly CancellationToken _ct;
        private readonly HashSet<IMethodSymbol> _visited;

        public KeyFieldResult? Result { get; private set; }

        public SymbolFlowTracer(
            ISymbol tracked, Compilation compilation,
            CancellationToken ct, HashSet<IMethodSymbol> visited)
        {
            _tracked = tracked;
            _compilation = compilation;
            _ct = ct;
            _visited = visited;
        }

        public override void VisitAssignmentStatement(IAssignmentStatement operation)
        {
            if (Result is not null) return;
            _ct.ThrowIfCancellationRequested();

            if (IsTrackedSymbol(operation.Value) && operation.Target is IFieldAccess fieldAccess)
            {
                Result = CheckFieldInKey(fieldAccess);
                if (Result is not null) return;
            }

            base.VisitAssignmentStatement(operation);
        }

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            if (Result is not null) return;
            _ct.ThrowIfCancellationRequested();

            if (IsValidateCall(operation) && operation.Arguments.Length >= 2 &&
                IsTrackedSymbol(operation.Arguments[1].Value))
            {
                Result = CheckValidateTarget(operation);
                if (Result is not null) return;
            }

            if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod &&
                !operation.TargetMethod.IsEvent)
            {
                for (int i = 0; i < operation.Arguments.Length; i++)
                {
                    if (IsTrackedSymbol(operation.Arguments[i].Value))
                    {
                        Result = TraceParameter(
                            operation.TargetMethod, i, _compilation, _ct, _visited);
                        if (Result is not null) return;
                    }
                }
            }

            base.VisitInvocationExpression(operation);
        }

        private bool IsTrackedSymbol(IOperation operation)
        {
            var op = UnwrapConversion(operation);
            var symbol = op.GetSymbolSafe();
            return symbol is not null && symbol.Equals(_tracked);
        }
    }

    #endregion

    #region Shared helpers

    private static bool IsValidateCall(IInvocationExpression invocation) =>
        invocation.TargetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod &&
        string.Equals(invocation.TargetMethod.Name, "Validate", StringComparison.OrdinalIgnoreCase);

    private static KeyFieldResult? CheckFieldInKey(IFieldAccess fieldAccess)
    {
        var fieldSymbol = fieldAccess.FieldSymbol;
        if (fieldSymbol is null)
            return null;

        if (fieldSymbol.GetTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Guid)
            return null;

        if (fieldAccess.Instance?.Type is not IRecordTypeSymbol recordType || recordType.Temporary)
            return null;

        if (recordType.OriginalDefinition is not ITableTypeSymbol tableType ||
            tableType.TableType != EnumProvider.TableTypeKind.Normal)
            return null;

        return IsFieldInAnyKey(fieldSymbol, tableType)
            ? new KeyFieldResult(fieldSymbol.Name, tableType.Name)
            : null;
    }

    private static KeyFieldResult? CheckValidateTarget(IInvocationExpression validateCall)
    {
        if (validateCall.Instance?.Type is not IRecordTypeSymbol recordType || recordType.Temporary)
            return null;

        if (recordType.OriginalDefinition is not ITableTypeSymbol tableType ||
            tableType.TableType != EnumProvider.TableTypeKind.Normal)
            return null;

        var firstArg = UnwrapConversion(validateCall.Arguments[0].Value);

        if (firstArg.GetSymbolSafe() is not IFieldSymbol fieldSymbol)
            return null;

        if (fieldSymbol.GetTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Guid)
            return null;

        return IsFieldInAnyKey(fieldSymbol, tableType)
            ? new KeyFieldResult(fieldSymbol.Name, tableType.Name)
            : null;
    }

    private static bool IsFieldInAnyKey(IFieldSymbol field, ITableTypeSymbol table)
    {
        foreach (var key in table.Keys)
        {
            foreach (var keyField in key.Fields)
            {
                if (string.Equals(keyField.Name, field.Name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private static IOperation UnwrapConversion(IOperation operation) =>
        operation is IConversionExpression conv ? conv.Operand : operation;

    #endregion
}
