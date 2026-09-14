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
        context.RegisterCompilationStartAction(start => start.RegisterCodeBlockAction(ctx => AnalyzeCodeBlock(ctx, start.Compilation)));

    private static void AnalyzeCodeBlock(CodeBlockAnalysisContext context, Compilation compilation)
    {
        if (context.IsObsolete() ||
            context.CodeBlock is not MethodOrTriggerDeclarationSyntax methodOrTrigger)
            return;

        var body = methodOrTrigger.Body;
        if (body is null)
            return;

        var settings = ALCopsSettingsProvider.GetSettings(compilation, context.CancellationToken);
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

    private static string KeyFieldReason(KeyFieldResult result) =>
        $"The value flows to key field '{result.FieldName}' in table '{result.TableName}'.";

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

            var value = operation.Value.UnwrapConversions();
            if (IsCreateGuidCall(value, out var createGuidInvocation))
            {
                if (_flagAllGuidFields)
                {
                    ReportDiagnostic(_context, createGuidInvocation,
                        "Sequential GUIDs improve index performance.");
                }
                else if (operation.Target is IFieldAccess fieldAccess)
                {
                    var result = CheckFieldInKey(fieldAccess, _context.OwningSymbol);
                    if (result is not null)
                    {
                        ReportDiagnostic(_context, createGuidInvocation,
                            KeyFieldReason(result.Value));
                    }
                }
                else
                {
                    var targetSymbol = operation.Target?.GetSymbolSafe();
                    if (targetSymbol is null)
                    {
                        // Unresolvable target; nothing to trace
                    }
                    else if (targetSymbol.Kind == EnumProvider.SymbolKind.LocalVariable)
                    {
                        var bodyOp = _context.SemanticModel.GetOperation(
                            ((MethodOrTriggerDeclarationSyntax)_context.CodeBlock).Body!,
                            _ct);
                        if (bodyOp is not null)
                        {
                            var result = TraceVariable(
                                targetSymbol, bodyOp, _context.SemanticModel.Compilation,
                                new HashSet<IMethodSymbol>(), _ct);
                            if (result is not null)
                            {
                                ReportDiagnostic(_context, createGuidInvocation,
                                    KeyFieldReason(result.Value));
                            }
                        }
                    }
                    else if (targetSymbol.Kind == EnumProvider.SymbolKind.GlobalVariable)
                    {
                        var result = TraceGlobalVariable(targetSymbol);
                        if (result is not null)
                        {
                            ReportDiagnostic(_context, createGuidInvocation,
                                KeyFieldReason(result.Value));
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
                var argValue = operation.Arguments[i].Value.UnwrapConversions();
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
                    var result = CheckValidateTarget(operation, _context.OwningSymbol);
                    if (result is not null)
                    {
                        ReportDiagnostic(_context, createGuidInvocation,
                            KeyFieldReason(result.Value));
                    }
                    continue;
                }

                // User procedure argument (skip events and built-in methods)
                if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod &&
                    !operation.TargetMethod.IsEvent)
                {
                    var result = TraceParameter(
                        operation.TargetMethod, i, _context.SemanticModel.Compilation,
                        new HashSet<IMethodSymbol>(), _ct);
                    if (result is not null)
                    {
                        ReportDiagnostic(_context, createGuidInvocation,
                            KeyFieldReason(result.Value));
                    }
                }
            }

            base.VisitInvocationExpression(operation);
        }

        private KeyFieldResult? TraceGlobalVariable(ISymbol globalVariable)
        {
            var objectSyntax = _context.CodeBlock.FirstAncestorOrSelf<ObjectSyntax>();
            if (objectSyntax is null)
                return null;

            foreach (var member in objectSyntax.DescendantNodes().OfType<MethodOrTriggerDeclarationSyntax>())
            {
                if (member.Body is null)
                    continue;

                _ct.ThrowIfCancellationRequested();

                // Text pre-filter: a body that never spells the variable name cannot reference it,
                // so skip the bind. False positives only cost one bind; the tracer decides by symbol.
                if (member.Body.ToString().IndexOf(globalVariable.Name, SemanticFacts.NameEqualityComparison) < 0)
                    continue;

                var bodyOp = _context.SemanticModel.GetOperation(member.Body, _ct);
                if (bodyOp is null)
                    continue;

                var containing = _context.SemanticModel.GetDeclaredSymbol(member) as ISymbol;
                var tracer = new SymbolFlowTracer(
                    globalVariable, _context.SemanticModel.Compilation,
                    new HashSet<IMethodSymbol>(), _ct, containing);
                tracer.Visit(bodyOp);

                if (tracer.Result is not null)
                    return tracer.Result;
            }

            return null;
        }

        private static bool IsCreateGuidCall(
            IOperation operation, out IInvocationExpression invocation)
        {
            if (operation is IInvocationExpression inv &&
                inv.TargetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod &&
                SemanticFacts.IsSameName(inv.TargetMethod.Name, "CreateGuid") &&
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
        HashSet<IMethodSymbol> visited, CancellationToken ct)
    {
        var tracer = new SymbolFlowTracer(variable, compilation, visited, ct);
        tracer.Visit(methodBody);
        return tracer.Result;
    }

    private static KeyFieldResult? TraceParameter(
        IMethodSymbol method, int paramIndex, Compilation compilation,
        HashSet<IMethodSymbol> visited, CancellationToken ct)
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
            var tracer = new SymbolFlowTracer(parameter, compilation, visited, ct);
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
        private readonly ISymbol? _containingSymbol;

        public KeyFieldResult? Result { get; private set; }

        public SymbolFlowTracer(
            ISymbol tracked, Compilation compilation,
            HashSet<IMethodSymbol> visited, CancellationToken ct,
            ISymbol? containingSymbol = null)
        {
            _tracked = tracked;
            _compilation = compilation;
            _ct = ct;
            _visited = visited;
            _containingSymbol = containingSymbol ?? tracked.ContainingSymbol;
        }

        public override void VisitAssignmentStatement(IAssignmentStatement operation)
        {
            if (Result is not null) return;
            _ct.ThrowIfCancellationRequested();

            if (IsTrackedSymbol(operation.Value) && operation.Target is IFieldAccess fieldAccess)
            {
                Result = CheckFieldInKey(fieldAccess, _containingSymbol);
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
                Result = CheckValidateTarget(operation, _containingSymbol);
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
                            operation.TargetMethod, i, _compilation, _visited, _ct);
                        if (Result is not null) return;
                    }
                }
            }

            base.VisitInvocationExpression(operation);
        }

        private bool IsTrackedSymbol(IOperation operation)
        {
            var op = operation.UnwrapConversions();
            var symbol = op.GetSymbolSafe();
            return symbol is not null && symbol.Equals(_tracked);
        }
    }

    #endregion

    #region Shared helpers

    private static bool IsValidateCall(IInvocationExpression invocation) =>
        invocation.TargetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod &&
        SemanticFacts.IsSameName(invocation.TargetMethod.Name, "Validate");

    private static KeyFieldResult? CheckFieldInKey(IFieldAccess fieldAccess, ISymbol? containingSymbol = null)
    {
        var fieldSymbol = fieldAccess.FieldSymbol;
        if (fieldSymbol is null)
            return null;

        if (fieldSymbol.GetTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Guid)
            return null;

        var tableType = fieldAccess.GetReceiverTableType(containingSymbol, out var recordType);
        if (tableType is null || tableType.TableType != EnumProvider.TableTypeKind.Normal)
            return null;

        if (recordType is not null && recordType.Temporary)
            return null;

        return IsFieldInAnyKey(fieldSymbol, tableType)
            ? new KeyFieldResult(fieldSymbol.Name, tableType.Name)
            : null;
    }

    private static KeyFieldResult? CheckValidateTarget(IInvocationExpression validateCall, ISymbol? containingSymbol = null)
    {
        var tableType = validateCall.GetReceiverTableType(containingSymbol, out var recordType);
        if (tableType is null || tableType.TableType != EnumProvider.TableTypeKind.Normal)
            return null;

        if (recordType is not null && recordType.Temporary)
            return null;

        var firstArg = validateCall.Arguments[0].Value.UnwrapConversions();

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
        // Keys holds declared keys only; a table without a keys section exposes
        // its synthesized primary key solely through PrimaryKey.
        var primaryKey = table.PrimaryKey;
        if (primaryKey is not null)
        {
            foreach (var keyField in primaryKey.Fields)
            {
                if (SemanticFacts.IsSameName(keyField.Name, field.Name))
                    return true;
            }
        }

        foreach (var key in table.Keys)
        {
            foreach (var keyField in key.Fields)
            {
                if (SemanticFacts.IsSameName(keyField.Name, field.Name))
                    return true;
            }
        }

        return false;
    }

    #endregion
}
