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

        var finalStates = AnalyzeOperation(
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

    private static ImmutableHashSet<bool> AnalyzeOperation(
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
                var trueStates = AnalyzeOperation(
                    ifStatement.IfTrueStatement,
                    states,
                    hasNamedReturn,
                    returnVariableName,
                    out var truePathWithoutValue);

                var falsePathWithoutValue = false;

                var falseStates = ifStatement.IfFalseStatement is null
                    ? states
                    : AnalyzeOperation(
                        ifStatement.IfFalseStatement,
                        states,
                        hasNamedReturn,
                        returnVariableName,
                        out falsePathWithoutValue);

                hasPathWithoutValue = truePathWithoutValue || falsePathWithoutValue;

                return trueStates.Union(falseStates);

            case ICaseStatement caseStatement:
                var mergedStates = ImmutableHashSet<bool>.Empty;
                var caseHasPathWithoutValue = false;

                foreach (var caseLine in caseStatement.CaseLines)
                {
                    var caseLineStates = AnalyzeCaseLine(
                        caseLine,
                        states,
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
                        states,
                        hasNamedReturn,
                        returnVariableName,
                        out var elseHasPathWithoutValue);

                    caseHasPathWithoutValue |= elseHasPathWithoutValue;
                    mergedStates = mergedStates.Union(elseStates);
                }
                else
                {
                    mergedStates = mergedStates.Union(states);
                }

                hasPathWithoutValue = caseHasPathWithoutValue;

                return mergedStates;

            case IWhileRepeatLoopStatement loopStatement:
                var bodyStates = AnalyzeOperation(
                    loopStatement.Body,
                    states,
                    hasNamedReturn,
                    returnVariableName,
                    out var loopHasPathWithoutValue);

                hasPathWithoutValue = loopHasPathWithoutValue;

                if (loopStatement.LoopKind == EnumProvider.LoopKind.Repeat)
                {
                    return bodyStates;
                }

                return states.Union(bodyStates);

            case IForLoopStatement forLoop:
                var forBodyStates = AnalyzeOperation(
                    forLoop.Body,
                    states,
                    hasNamedReturn,
                    returnVariableName,
                    out var forHasPathWithoutValue);

                hasPathWithoutValue = forHasPathWithoutValue;

                return states.Union(forBodyStates);

            case IForEachLoopStatement forEachLoop:
                var forEachBodyStates = AnalyzeOperation(
                    forEachLoop.Body,
                    states,
                    hasNamedReturn,
                    returnVariableName,
                    out var forEachHasPathWithoutValue);

                hasPathWithoutValue = forEachHasPathWithoutValue;

                return states.Union(forEachBodyStates);

            default:
                return states;
        }
    }

    private static ImmutableHashSet<bool> AnalyzeStatements(
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

    private static ImmutableHashSet<bool> AnalyzeCaseLine(
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
