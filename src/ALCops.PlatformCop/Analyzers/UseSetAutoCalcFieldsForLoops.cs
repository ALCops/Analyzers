using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class UseSetAutoCalcFieldsForLoops : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> FindSetMethods =
        ImmutableHashSet.Create(SemanticFacts.NameEqualityComparer, "FindSet", "Find");

    private static readonly ImmutableHashSet<string> NextMethods =
        ImmutableHashSet.Create(SemanticFacts.NameEqualityComparer, "Next");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.UseSetAutoCalcFieldsForLoops);

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

        var operation = context.SemanticModel.GetOperation(body, context.CancellationToken);
        if (operation is null)
            return;

        // Determine if this is a report OnAfterGetRecord trigger
        string? implicitLoopVariable = GetImplicitLoopVariable(methodSymbol);

        var walker = new CalcFieldsInLoopWalker(implicitLoopVariable, context.CancellationToken);
        walker.Visit(operation);

        foreach (var diagnostic in walker.Diagnostics)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UseSetAutoCalcFieldsForLoops,
                diagnostic.Location,
                diagnostic.VariableName));
        }
    }

    /// <summary>
    /// For report DataItem OnAfterGetRecord triggers, the entire body is implicitly a loop
    /// on the DataItem's record variable.
    /// </summary>
    private static string? GetImplicitLoopVariable(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.MethodKind != EnumProvider.MethodKind.Trigger)
            return null;

        if (!SemanticFacts.IsSameName(methodSymbol.Name, "OnAfterGetRecord"))
            return null;

        if (methodSymbol.ContainingSymbol is not IReportDataItemSymbol dataItem)
            return null;

        return dataItem.Name;
    }

    private sealed class CalcFieldsDiagnosticInfo(Location location, string variableName)
    {
        public Location Location { get; } = location;
        public string VariableName { get; } = variableName;
    }

    private sealed class CalcFieldsInLoopWalker : OperationWalker
    {
        private readonly CancellationToken _cancellationToken;
        private readonly string? _implicitLoopVariable;
        private readonly List<CalcFieldsDiagnosticInfo> _diagnostics = new();

        // Stack of loop variable names. When inside a loop, this contains the
        // variable(s) driving the current loop scope.
        private readonly Stack<ImmutableHashSet<string>> _loopVariables = new();

        // Tracks nesting depth inside conditional branches (if/case) relative to
        // the current loop. Reset to 0 when entering a new loop body.
        private int _conditionalDepth;
        private readonly Stack<int> _savedConditionalDepths = new();

        public IReadOnlyList<CalcFieldsDiagnosticInfo> Diagnostics => _diagnostics;

        public CalcFieldsInLoopWalker(string? implicitLoopVariable, CancellationToken cancellationToken)
        {
            _implicitLoopVariable = implicitLoopVariable;
            _cancellationToken = cancellationToken;

            // If this is an OnAfterGetRecord trigger, the entire body is an implicit loop
            if (implicitLoopVariable is not null)
            {
                _loopVariables.Push(
                    ImmutableHashSet.Create(SemanticFacts.NameEqualityComparer, implicitLoopVariable));
            }
        }

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (IsInLoop() && _conditionalDepth == 0 && IsCalcFieldsCall(operation))
            {
                var instanceName = GetInstanceVariableName(operation);
                if (instanceName is not null && IsLoopVariable(instanceName))
                {
                    _diagnostics.Add(new CalcFieldsDiagnosticInfo(
                        operation.Syntax.GetLocation(),
                        instanceName));
                }
            }

            base.VisitInvocationExpression(operation);
        }

        public override void VisitWhileRepeatLoopStatement(IWhileRepeatLoopStatement operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (operation.LoopKind == EnumProvider.LoopKind.Repeat)
            {
                // repeat-until: loop variable is the one that calls Next() in the condition
                var loopVar = ExtractNextVariableFromCondition(operation.Condition);
                var loopVars = loopVar is not null
                    ? ImmutableHashSet.Create(SemanticFacts.NameEqualityComparer, loopVar)
                    : ImmutableHashSet<string>.Empty;

                PushLoop(loopVars);
                Visit(operation.Body);
                Visit(operation.Condition);
                PopLoop();
            }
            else
            {
                // while-do: loop variable could be in the condition (FindSet/Find)
                var loopVar = ExtractFindSetVariableFromCondition(operation.Condition);
                var loopVars = loopVar is not null
                    ? ImmutableHashSet.Create(SemanticFacts.NameEqualityComparer, loopVar)
                    : ImmutableHashSet<string>.Empty;

                PushLoop(loopVars);
                Visit(operation.Condition);
                Visit(operation.Body);
                PopLoop();
            }
        }

        public override void VisitForEachLoopStatement(IForEachLoopStatement operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var iterVarName = GetIterationVariableName(operation);
            var loopVars = iterVarName is not null
                ? ImmutableHashSet.Create(SemanticFacts.NameEqualityComparer, iterVarName)
                : ImmutableHashSet<string>.Empty;

            PushLoop(loopVars);
            Visit(operation.Expression);
            Visit(operation.Body);
            PopLoop();
        }

        public override void VisitForLoopStatement(IForLoopStatement operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            // For loops don't typically loop over records, but we still walk the body
            // with an empty loop variable set (CalcFields won't match any loop var)
            PushLoop(ImmutableHashSet<string>.Empty);
            Visit(operation.InitialValue);
            Visit(operation.EndValue);
            Visit(operation.Body);
            PopLoop();
        }

        public override void VisitIfStatement(IIfStatement operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (!IsInLoop())
            {
                base.VisitIfStatement(operation);
                return;
            }

            // Inside a loop: walk branches with incremented conditional depth.
            // This suppresses direct CalcFields detection but still discovers nested loops.
            _conditionalDepth++;
            Visit(operation.Condition);
            Visit(operation.IfTrueStatement);
            Visit(operation.IfFalseStatement);
            _conditionalDepth--;
        }

        public override void VisitCaseStatement(ICaseStatement operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (!IsInLoop())
            {
                base.VisitCaseStatement(operation);
                return;
            }

            // Inside a loop: walk case lines with incremented conditional depth.
            _conditionalDepth++;
            Visit(operation.Value);
            foreach (var caseLine in operation.CaseLines)
                Visit(caseLine);
            if (operation.ElseStatement is not null)
                Visit(operation.ElseStatement);
            _conditionalDepth--;
        }

        private bool IsInLoop() => _loopVariables.Count > 0;

        private void PushLoop(ImmutableHashSet<string> loopVars)
        {
            // Save current conditional depth on the stack (encoded with the loop vars)
            // and reset to 0: inside a new loop, code is unconditional relative to that loop.
            _savedConditionalDepths.Push(_conditionalDepth);
            _conditionalDepth = 0;
            _loopVariables.Push(loopVars);
        }

        private void PopLoop()
        {
            _loopVariables.Pop();
            _conditionalDepth = _savedConditionalDepths.Pop();
        }

        private bool IsLoopVariable(string variableName)
        {
            foreach (var loopVarSet in _loopVariables)
            {
                if (loopVarSet.Contains(variableName))
                    return true;
            }
            return false;
        }

        private static bool IsCalcFieldsCall(IInvocationExpression invocation)
        {
            var targetMethod = invocation.TargetMethod;
            if (targetMethod is null)
                return false;

            return targetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod &&
                   SemanticFacts.IsSameName(targetMethod.Name, "CalcFields");
        }

        private static string? GetInstanceVariableName(IInvocationExpression invocation)
        {
            return invocation.Instance?.GetSymbolSafe()?.Name;
        }

        /// <summary>
        /// Extracts the variable calling Next() from a repeat-until condition.
        /// Pattern: "Rec.Next() = 0" or "Rec.Next() &lt; 1"
        /// </summary>
        private static string? ExtractNextVariableFromCondition(IOperation? condition)
        {
            if (condition is null)
                return null;

            // Walk the condition to find a Next() call
            var finder = new MethodCallFinder(NextMethods);
            finder.Visit(condition);
            return finder.FoundVariableName;
        }

        /// <summary>
        /// Extracts the variable calling FindSet/Find from a while condition.
        /// Pattern: "Rec.FindSet()" or "Rec.Find('-')"
        /// </summary>
        private static string? ExtractFindSetVariableFromCondition(IOperation? condition)
        {
            if (condition is null)
                return null;

            var finder = new MethodCallFinder(FindSetMethods);
            finder.Visit(condition);
            return finder.FoundVariableName;
        }

        private static string? GetIterationVariableName(IForEachLoopStatement forEachLoop)
        {
            if (forEachLoop.IterationVariable is null)
                return null;

            var symbol = forEachLoop.IterationVariable.GetSymbolSafe();
            return symbol?.Name;
        }
    }

    /// <summary>
    /// Finds a built-in method call (from a set of method names) in an operation tree
    /// and extracts the instance variable name.
    /// </summary>
    private sealed class MethodCallFinder : OperationWalker
    {
        private readonly ImmutableHashSet<string> _methodNames;
        public string? FoundVariableName { get; private set; }

        public MethodCallFinder(ImmutableHashSet<string> methodNames)
        {
            _methodNames = methodNames;
        }

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            if (FoundVariableName is not null)
                return;

            var targetMethod = operation.TargetMethod;
            if (targetMethod is not null &&
                targetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod &&
                _methodNames.Contains(targetMethod.Name))
            {
                FoundVariableName = operation.Instance?.GetSymbolSafe()?.Name;
            }

            base.VisitInvocationExpression(operation);
        }
    }
}
