using System.Collections;
using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class NotAllCodePathsReturnValue : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.NotAllCodePathsReturnValue);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            AnalyzeDeclaration,
            EnumProvider.SyntaxKind.MethodDeclaration);
    // Can be extended to triggers in the future: EnumProvider.SyntaxKind.TriggerDeclaration

    private static void AnalyzeDeclaration(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not MethodDeclarationSyntax declarationSyntax)
        {
            return;
        }

        if (declarationSyntax.ReturnValue is null)
        {
            return;
        }

        if (declarationSyntax is MethodDeclarationSyntax methodSyntax && methodSyntax.IsTryFunction())
        {
            return;
        }

        if (ctx.ContainingSymbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        var returnValue = methodSymbol.ReturnValueSymbol;

        if (returnValue is null)
        {
            return;
        }

        if (declarationSyntax.Body is null)
        {
            return;
        }

        var bodyOperation = ctx.SemanticModel.GetOperation(declarationSyntax.Body, ctx.CancellationToken);

        if (bodyOperation is null)
        {
            return;
        }

        var hasNamedReturn = returnValue.IsNamed;

        var flowAnalyzer = new FlowAnalyzer(ctx.SemanticModel, ctx.CancellationToken);

        var finalStates = flowAnalyzer.AnalyzeOperation(
            bodyOperation,
            ImmutableHashSet.Create(false),
            hasNamedReturn,
            returnValue.Name,
            out var hasPathWithoutValue);

        var hasFallthroughWithoutValue = hasNamedReturn
            ? finalStates.Contains(false)
            : finalStates.Count > 0;

        if (!hasPathWithoutValue && !hasFallthroughWithoutValue)
        {
            return;
        }

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.NotAllCodePathsReturnValue,
            declarationSyntax.Name.GetLocation(),
            methodSymbol.GetDiagnosticDisplayText(MethodSymbolDisplayFormat.MethodSignature)));
    }

    private sealed class FlowAnalyzer(SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        private readonly SemanticModel _semanticModel = semanticModel;
        private readonly CancellationToken _cancellationToken = cancellationToken;

        public ImmutableHashSet<bool> AnalyzeOperation(
            IOperation? operation,
            ImmutableHashSet<bool> states,
            bool hasNamedReturn,
            string returnVariableName,
            out bool hasPathWithoutValue)
        {
            hasPathWithoutValue = false;

            if (operation is null || states.Count == 0)
            {
                return states;
            }

            switch (operation)
            {
                case IBlockStatement block:
                    return AnalyzeStatements(block.Statements, states, hasNamedReturn, returnVariableName, out hasPathWithoutValue);

                case IStatementList statementList:
                    return AnalyzeStatements(statementList.Statements, states, hasNamedReturn, returnVariableName, out hasPathWithoutValue);

                case IAssignmentStatement assignment:
                    if (hasNamedReturn && assignment.Target.IsNamedReturnTarget(returnVariableName))
                    {
                        return ImmutableHashSet.Create(true);
                    }

                    return states;

                case IExitStatement exitStatement:
                    var returnsValue = exitStatement.ReturnedValue is not null;

                    if (!returnsValue)
                    {
                        if (!hasNamedReturn)
                        {
                            hasPathWithoutValue = true;
                        }
                        else
                        {
                            foreach (var assigned in states)
                            {
                                if (!assigned)
                                {
                                    hasPathWithoutValue = true;
                                    break;
                                }
                            }
                        }
                    }

                    return ImmutableHashSet<bool>.Empty;

                case IIfStatement ifStatement:
                    var conditionStates = AnalyzeCondition(
                        ifStatement.Condition,
                        states,
                        hasNamedReturn,
                        returnVariableName);

                    var trueStates = AnalyzeOperation(
                        ifStatement.IfTrueStatement,
                        conditionStates,
                        hasNamedReturn,
                        returnVariableName,
                        out var truePathWithoutValue);

                    var falsePathWithoutValue = false;

                    var falseStates = ifStatement.IfFalseStatement is null
                        ? conditionStates
                        : AnalyzeOperation(
                            ifStatement.IfFalseStatement,
                            conditionStates,
                            hasNamedReturn,
                            returnVariableName,
                            out falsePathWithoutValue);

                    hasPathWithoutValue = truePathWithoutValue || falsePathWithoutValue;

                    return trueStates.Union(falseStates);

                case ICaseStatement caseStatement:
                    // Case selector is evaluated exactly once, no short-circuit; treat like an if condition.
                    var caseSelectorStates = AnalyzeCondition(
                        caseStatement.Value,
                        states,
                        hasNamedReturn,
                        returnVariableName);

                    if (caseSelectorStates.Count == 0)
                    {
                        return caseSelectorStates;
                    }

                    var mergedStates = ImmutableHashSet<bool>.Empty;
                    var caseHasPathWithoutValue = false;

                    foreach (var caseLine in caseStatement.CaseLines)
                    {
                        var caseLineStates = AnalyzeCaseLine(
                            caseLine,
                            caseSelectorStates,
                            hasNamedReturn,
                            returnVariableName,
                            out var caseLineHasPathWithoutValue);

                        caseHasPathWithoutValue |= caseLineHasPathWithoutValue;
                        mergedStates = mergedStates.Union(caseLineStates);
                    }

                    if (caseStatement.ElseStatement is not null)
                    {
                        var elseStates = AnalyzeOperation(
                            caseStatement.ElseStatement,
                            caseSelectorStates,
                            hasNamedReturn,
                            returnVariableName,
                            out var elseHasPathWithoutValue);

                        caseHasPathWithoutValue |= elseHasPathWithoutValue;
                        mergedStates = mergedStates.Union(elseStates);
                    }
                    else if (!IsExhaustiveCase(caseStatement))
                    {
                        mergedStates = mergedStates.Union(caseSelectorStates);
                    }

                    hasPathWithoutValue = caseHasPathWithoutValue;

                    return mergedStates;

                case IWhileRepeatLoopStatement loopStatement:
                    if (loopStatement.LoopKind == EnumProvider.LoopKind.Repeat)
                    {
                        // repeat-until: body runs at least once, then the condition is evaluated at least once.
                        var repeatBodyStates = AnalyzeOperation(
                            loopStatement.Body,
                            states,
                            hasNamedReturn,
                            returnVariableName,
                            out var repeatHasPathWithoutValue);

                        hasPathWithoutValue = repeatHasPathWithoutValue;

                        if (repeatBodyStates.Count == 0)
                        {
                            return repeatBodyStates;
                        }

                        return AnalyzeCondition(
                            loopStatement.Condition,
                            repeatBodyStates,
                            hasNamedReturn,
                            returnVariableName);
                    }

                    // while-do: condition is evaluated at least once before the body, body may not run.
                    var whileConditionStates = AnalyzeCondition(
                        loopStatement.Condition,
                        states,
                        hasNamedReturn,
                        returnVariableName);

                    if (whileConditionStates.Count == 0)
                    {
                        return whileConditionStates;
                    }

                    var bodyStates = AnalyzeOperation(
                        loopStatement.Body,
                        whileConditionStates,
                        hasNamedReturn,
                        returnVariableName,
                        out var loopHasPathWithoutValue);

                    hasPathWithoutValue = loopHasPathWithoutValue;

                    return whileConditionStates.Union(bodyStates);

                case IForLoopStatement forLoop:
                    // for i := from to to do: from and to are evaluated at least once, body may not run.
                    var forFromStates = AnalyzeCondition(
                        forLoop.InitialValue,
                        states,
                        hasNamedReturn,
                        returnVariableName);

                    if (forFromStates.Count == 0)
                    {
                        return forFromStates;
                    }

                    var forRangeStates = AnalyzeCondition(
                        forLoop.EndValue,
                        forFromStates,
                        hasNamedReturn,
                        returnVariableName);

                    if (forRangeStates.Count == 0)
                    {
                        return forRangeStates;
                    }

                    var forBodyStates = AnalyzeOperation(
                        forLoop.Body,
                        forRangeStates,
                        hasNamedReturn,
                        returnVariableName,
                        out var forHasPathWithoutValue);

                    hasPathWithoutValue = forHasPathWithoutValue;

                    return forRangeStates.Union(forBodyStates);

                case IForEachLoopStatement forEachLoop:
                    // foreach x in collection: collection expression is evaluated once, body may not run.
                    var forEachExprStates = AnalyzeCondition(
                        forEachLoop.Expression,
                        states,
                        hasNamedReturn,
                        returnVariableName);

                    if (forEachExprStates.Count == 0)
                    {
                        return forEachExprStates;
                    }

                    var forEachBodyStates = AnalyzeOperation(
                        forEachLoop.Body,
                        forEachExprStates,
                        hasNamedReturn,
                        returnVariableName,
                        out var forEachHasPathWithoutValue);

                    hasPathWithoutValue = forEachHasPathWithoutValue;

                    return forEachExprStates.Union(forEachBodyStates);

                case IInvocationExpression invocation:
                    return AnalyzeInvocation(invocation, states, hasNamedReturn, returnVariableName);

                case IExpressionStatement expressionStatement
                    when expressionStatement.Expression is IInvocationExpression wrappedInvocation:
                    return AnalyzeInvocation(wrappedInvocation, states, hasNamedReturn, returnVariableName);

                default:
                    return states;
            }
        }

        private static ImmutableHashSet<bool> AnalyzeInvocation(
            IInvocationExpression invocation,
            ImmutableHashSet<bool> states,
            bool hasNamedReturn,
            string returnVariableName)
        {
            if (IsFlowTerminatingCall(invocation))
            {
                return ImmutableHashSet<bool>.Empty;
            }

            if (hasNamedReturn && InvocationAssignsNamedReturn(invocation, returnVariableName))
            {
                return ImmutableHashSet.Create(true);
            }

            return states;
        }

        private bool IsExhaustiveCase(ICaseStatement caseStatement)
        {
            var selectorType = GetCaseSelectorType(caseStatement.Value.UnwrapConversions());

            return selectorType switch
            {
                IEnumBaseTypeSymbol enumType => IsExhaustiveCase(
                    caseStatement,
                    enumType.Values.Select(static enumValue => enumValue.Ordinal)),
                IOptionTypeSymbol optionType => IsExhaustiveCase(
                    caseStatement,
                    optionType.Values.Select(static optionValue => optionValue.Ordinal)),
                IContainerSymbol container => IsExhaustiveCase(
                    caseStatement,
                    container.GetMembers().OfType<IOptionSymbol>().Select(static optionValue => optionValue.Ordinal)),
                _ => false
            };
        }

        private ITypeSymbol? GetCaseSelectorType(IOperation selector)
        {
            if (selector.Type is not null)
            {
                return selector.Type;
            }

            var selectorSymbol = GetOperationSymbol(selector);

            return selectorSymbol switch
            {
                IParameterSymbol parameter => parameter.ParameterType,
                IVariableSymbol variable => variable.Type,
                IReturnValueSymbol returnValue => returnValue.ReturnType,
                _ => null
            };
        }

        private bool IsExhaustiveCase(ICaseStatement caseStatement, IEnumerable<int> possibleOrdinals)
        {
            var expectedOrdinals = possibleOrdinals.ToImmutableHashSet();

            if (expectedOrdinals.Count == 0)
            {
                return false;
            }

            var handledOrdinals = ImmutableHashSet<int>.Empty;

            foreach (var caseLine in caseStatement.CaseLines)
            {
                foreach (var expression in caseLine.Expressions)
                {
                    if (GetCaseLabelOrdinal(expression) is int ordinal)
                    {
                        handledOrdinals = handledOrdinals.Add(ordinal);
                    }
                }
            }

            return expectedOrdinals.IsSubsetOf(handledOrdinals);
        }

        private int? GetCaseLabelOrdinal(IOperation expression)
        {
            return GetCaseLabelSymbol(expression) switch
            {
                IEnumValueSymbol enumValue => enumValue.Ordinal,
                IOptionSymbol optionValue => optionValue.Ordinal,
                _ => null
            };
        }

        private ISymbol? GetCaseLabelSymbol(IOperation expression)
        {
            return GetOperationSymbol(expression);
        }

        private ISymbol? GetOperationSymbol(IOperation operation)
        {
            var unwrappedOperation = operation.UnwrapConversions();
            var operationSymbol = (unwrappedOperation as IOptionAccess)?.OptionSymbol
                ?? unwrappedOperation.GetSymbolSafe();

            if (operationSymbol is not null)
            {
                return operationSymbol;
            }

            try
            {
                return _semanticModel.GetSymbolInfo(unwrappedOperation.Syntax, _cancellationToken).Symbol;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static ImmutableHashSet<bool> AnalyzeCondition(
            IOperation condition,
            ImmutableHashSet<bool> states,
            bool hasNamedReturn,
            string returnVariableName)
        {
            condition = condition.UnwrapConversions();

            if (condition is IBinaryOperatorExpression binaryExpression
                && (condition.Syntax.IsKind(EnumProvider.SyntaxKind.LogicalAndExpression)
                    || condition.Syntax.IsKind(EnumProvider.SyntaxKind.LogicalOrExpression)))
            {
                var leftStates = AnalyzeCondition(
                    binaryExpression.LeftOperand,
                    states,
                    hasNamedReturn,
                    returnVariableName);

                var rightStates = AnalyzeCondition(
                    binaryExpression.RightOperand,
                    leftStates,
                    hasNamedReturn,
                    returnVariableName);

                return leftStates.Union(rightStates);
            }

#if !NETSTANDARD2_1
            if (condition is IConditionalOperatorExpression conditionalExpression)
            {
                var conditionStates = AnalyzeCondition(
                    conditionalExpression.Condition,
                    states,
                    hasNamedReturn,
                    returnVariableName);

                var trueStates = AnalyzeCondition(
                    conditionalExpression.WhenTrue,
                    conditionStates,
                    hasNamedReturn,
                    returnVariableName);

                var falseStates = AnalyzeCondition(
                    conditionalExpression.WhenFalse,
                    conditionStates,
                    hasNamedReturn,
                    returnVariableName);

                return trueStates.Union(falseStates);
            }
#endif

            if (condition is IUnaryOperatorExpression unaryExpression)
            {
                return AnalyzeCondition(
                    unaryExpression.Operand,
                    states,
                    hasNamedReturn,
                    returnVariableName);
            }

            if (ContainsBranchingExpression(condition))
            {
                return states;
            }

            foreach (var operation in condition.DescendantsAndSelf())
            {
                if (operation is IInvocationExpression invocation)
                {
                    states = AnalyzeInvocation(invocation, states, hasNamedReturn, returnVariableName);

                    if (states.Count == 0)
                    {
                        break;
                    }
                }
            }

            return states;
        }

        // Skip conditions where an operand may not execute: short-circuit `and`/`or` or a
        // conditional (ternary-like) expression. Treating them as no-op keeps PC0038 conservative.
        private static bool ContainsBranchingExpression(IOperation condition) =>
            condition.Syntax.DescendantNodesAndSelf().Any(static node =>
                node.IsKind(EnumProvider.SyntaxKind.LogicalAndExpression) ||
                node.IsKind(EnumProvider.SyntaxKind.LogicalOrExpression) ||
                node.IsKind(EnumProvider.SyntaxKind.ConditionalExpression));

        // Named return is considered "assigned" when it is either the receiver of an invocation
        // (e.g. `Customer.Get(No)` where `Customer` is the return variable, populating the record)
        // or is passed as a by-reference (`var`) argument (e.g. `ComputeInto(Result)`).
        // This is intentionally conservative to avoid false positives on common AL idioms.
        private static bool InvocationAssignsNamedReturn(IInvocationExpression invocation, string returnVariableName)
        {
            if (invocation.Instance.IsNamedReturnTarget(returnVariableName))
            {
                return true;
            }

            foreach (var argument in invocation.Arguments)
            {
                if (argument.Parameter is IParameterSymbol parameter
                    && parameter.IsVar
                    && argument.Value.IsNamedReturnTarget(returnVariableName))
                {
                    return true;
                }
            }

            return false;
        }

        // Built-in AL methods that never return control to the caller (they throw).
        // Treating them as path terminators prevents PC0038 false positives on guard clauses
        // such as `if Cond then exit(x) else Error('...');`.
        private static bool IsFlowTerminatingCall(IInvocationExpression invocation)
        {
            if (invocation.TargetMethod is not IMethodSymbol targetMethod)
            {
                return false;
            }

            if (targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            {
                return false;
            }

            return string.Equals(targetMethod.Name, "Error", StringComparison.Ordinal)
                || string.Equals(targetMethod.Name, "ThrowError", StringComparison.Ordinal);
        }

        private ImmutableHashSet<bool> AnalyzeStatements(
            IEnumerable<IOperation> statements,
            ImmutableHashSet<bool> initialStates,
            bool hasNamedReturn,
            string returnVariableName,
            out bool hasPathWithoutValue)
        {
            var states = initialStates;
            var anyPathWithoutValue = false;

            foreach (var statement in statements)
            {
                states = AnalyzeOperation(
                    statement,
                    states,
                    hasNamedReturn,
                    returnVariableName,
                    out var statementHasPathWithoutValue);

                anyPathWithoutValue |= statementHasPathWithoutValue;

                if (states.Count == 0)
                {
                    break;
                }
            }

            hasPathWithoutValue = anyPathWithoutValue;

            return states;
        }

        private ImmutableHashSet<bool> AnalyzeCaseLine(
            object caseLine,
            ImmutableHashSet<bool> states,
            bool hasNamedReturn,
            string returnVariableName,
            out bool hasPathWithoutValue)
        {
            hasPathWithoutValue = false;

            if (caseLine is IOperation caseOperation)
            {
                var bodyOperation = caseOperation.GetPropertyIfExists<IOperation>("Body")
                    ?? caseOperation.GetPropertyIfExists<IOperation>("Statement");

                if (bodyOperation is not null)
                {
                    return AnalyzeOperation(
                        bodyOperation,
                        states,
                        hasNamedReturn,
                        returnVariableName,
                        out hasPathWithoutValue);
                }

                var statements = caseOperation.GetPropertyIfExists<IEnumerable>("Statements");

                if (statements is null)
                {
                    return states;
                }

                var result = states;

                foreach (var statement in statements)
                {
                    if (statement is not IOperation statementOperation)
                    {
                        continue;
                    }

                    result = AnalyzeOperation(
                        statementOperation,
                        result,
                        hasNamedReturn,
                        returnVariableName,
                        out var statementHasPathWithoutValue);

                    hasPathWithoutValue |= statementHasPathWithoutValue;

                    if (result.Count == 0)
                    {
                        break;
                    }
                }

                return result;
            }

            return states;
        }
    }
}
