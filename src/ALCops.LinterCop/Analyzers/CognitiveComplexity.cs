using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class CognitiveComplexity : DiagnosticAnalyzer
{
    private int complexityThreshold;
    private bool IsIncrementDiagnosticsEnabled;

    // Flow-Breaking Structures: These disrupt the linear execution of the code.
    // Each occurrence of these structures adds +1 complexity to the score.
    private static readonly HashSet<SyntaxKind> flowBreakingKinds = new()
    {
        EnumProvider.SyntaxKind.IfStatement,
        EnumProvider.SyntaxKind.CaseStatement,
        EnumProvider.SyntaxKind.ForStatement,
        EnumProvider.SyntaxKind.ForEachStatement,
        EnumProvider.SyntaxKind.WhileStatement,
        EnumProvider.SyntaxKind.RepeatStatement,
        EnumProvider.SyntaxKind.ConditionalExpression // Ternary operator
    };

    // Nested Structures: These introduce additional cognitive load due to nesting.
    // Unlike flow-breaking structures that always add complexity, nested structures only add an extra penalty when nested inside another structure.
    // Currently there's no difference between the Flow-Breaking Structures and Nested Structures in the AL Language.
    // For example in C# nestedStructures could contain try-catch-finally
    private static readonly HashSet<SyntaxKind> nestedStructures = new()
    {
        EnumProvider.SyntaxKind.IfStatement,
        EnumProvider.SyntaxKind.CaseStatement,
        EnumProvider.SyntaxKind.ForStatement,
        EnumProvider.SyntaxKind.ForEachStatement,
        EnumProvider.SyntaxKind.WhileStatement,
        EnumProvider.SyntaxKind.RepeatStatement,
        EnumProvider.SyntaxKind.ConditionalExpression // Ternary operator
    };

    // This HashSet defines specific identifiers that, in certain cases, restrict whether a statement qualifies as a guard clause.
    // Some exit commands (e.g., "Break", "Skip", "Quit") are only considered guard clauses if they are called on these identifiers.
    private static readonly HashSet<string> guardClauseIdentifiers = new(SemanticFacts.NameEqualityComparer)
    {
        "CurrReport",
        "CurrXMLport"
    };

    // This HashSet defines commands that act as guard clause exits, meaning they immediately alter the flow of execution.
    // These commands are typically used in scenarios where a function, loop, or process needs to be stopped or skipped under certain conditions.
    // However, "Exit" is not included in this set, as we can get the ExitStatementSyntax type directly on the Statement of the IfStatementSyntax
    private static readonly HashSet<string> guardClauseExitCommands = new(SemanticFacts.NameEqualityComparer)
    {
        "Break",
        "Continue",
        "Error",
        "Quit",
        "Skip"
    };

    private static readonly HashSet<string> eventPublisherDecoratorNames = new(SemanticFacts.NameEqualityComparer)
    {
        "BusinessEvent",
        "IntegrationEvent",
        "ExternalBusinessEvent"
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.CognitiveComplexityMetric,
            DiagnosticDescriptors.CognitiveComplexityIncrement,
            DiagnosticDescriptors.CognitiveComplexityThresholdExceeded);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var compilation = compilationContext.Compilation;
            this.complexityThreshold = LoadCognitiveComplexityThreshold(compilation);
            this.IsIncrementDiagnosticsEnabled = compilation.IsDiagnosticEnabled(DiagnosticDescriptors.CognitiveComplexityIncrement);
            var recursion = new CognitiveComplexityRecursionGraphService(compilation);


            compilationContext.RegisterCodeBlockAction(codeBlockContext =>
            {
                AnalyzeCognitiveComplexity(codeBlockContext, recursion);
            });
        });
    }

    private void AnalyzeCognitiveComplexity(CodeBlockAnalysisContext context, CognitiveComplexityRecursionGraphService recursion)
    {
        if (context.IsObsolete() || context.CodeBlock is not MethodOrTriggerDeclarationSyntax methodOrTrigger)
            return;

        var containingObjectTypeSymbol = context.OwningSymbol.GetContainingObjectTypeSymbol();
        if (containingObjectTypeSymbol.NavTypeKind == EnumProvider.NavTypeKind.Interface ||
            containingObjectTypeSymbol.NavTypeKind == EnumProvider.NavTypeKind.ControlAddIn)
            return;

        if (methodOrTrigger.Body is null ||
            methodOrTrigger.Body.Statements.Count == 0 &&
            methodOrTrigger.Attributes.Any(attr => eventPublisherDecoratorNames.Contains(attr.GetIdentifierOrLiteralValue() ?? string.Empty)))
            return;

        int complexity = CalculateCognitiveComplexity(context, recursion, methodOrTrigger.Body);
        if (complexity >= complexityThreshold)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.CognitiveComplexityThresholdExceeded,
                context.OwningSymbol.GetLocation(),
                complexity,
                complexityThreshold));
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.CognitiveComplexityMetric,
            context.OwningSymbol.GetLocation(),
            complexity,
            complexityThreshold));
    }

    private int CalculateCognitiveComplexity(CodeBlockAnalysisContext context, CognitiveComplexityRecursionGraphService recursion, SyntaxNode root)
    {
        int complexity = 0;
        var stack = new Stack<(SyntaxNode node, int nestingLevel)>();
        stack.Push((root, 0));

        while (stack.Count > 0)
        {
            var (node, nestingLevel) = stack.Pop();

            if (node.IsKind(EnumProvider.SyntaxKind.IfStatement))
            {
                ProcessIfStatement(context, ref stack, node, ref complexity, ref nestingLevel);
                continue; // Skip further processing for this IF node
            }

            if (IsFlowBreakingStructure(node) && !IsGuardClause(node))
            {
                complexity += 1 + nestingLevel;
                RaiseIncrementDiagnostic(context, GetKeywordLocation(node, node.SpanStart), node.Kind.ToString(), nestingLevel);

                if (IsNestedStructure(node))
                    nestingLevel++;
            }

            foreach (var child in node.ChildNodes())
            {
                stack.Push((child, nestingLevel));
            }
        }

        if (context.CodeBlock.IsKind(EnumProvider.SyntaxKind.MethodDeclaration))
        {
            complexity += CalculateRecursionComplexity(context, recursion, root);
        }

        return complexity;
    }

    // The 'else if' increment causes a problem
    // In the AL Language 'else if' is an 'else" keyword followed by an 'if' node (not a single 'elsif' node).
    // If we increment for both 'else' and 'if' kinds the number will be too high.
    // So we'll increment for 'else' nodes not followed by an 'if' and rely on the 'if' to increment 'else if' statements.
    private void ProcessIfStatement(CodeBlockAnalysisContext context, ref Stack<(SyntaxNode, int)> stack, SyntaxNode node, ref int complexity, ref int nestingLevel)
    {
        if (node is not IfStatementSyntax ifStatement)
            return;

        if (!IsGuardClause(node))
        {
            // Increment for the 'if' statement
            complexity += 1 + nestingLevel;
            RaiseIncrementDiagnostic(context, GetKeywordLocation(node, node.SpanStart), node.Kind.ToString(), nestingLevel);
        }

        // Push the condition of the 'if' statement back to the stack
        stack.Push((ifStatement.Condition, nestingLevel));

        // Push the 'then' block with increased nesting
        if (ifStatement.Statement is not null)
            stack.Push((ifStatement.Statement, nestingLevel + 1));

        // Handle 'else' statement logic from 'if' statement
        if (ifStatement.ElseStatement is not null)
        {
            // 'else' not followed by 'if'
            if (ifStatement.ElseStatement is not IfStatementSyntax)
            {
                // Increment for the 'else' statement
                complexity += 1 + nestingLevel;
                RaiseIncrementDiagnostic(context, ifStatement.ElseKeywordToken.GetLocation(), "ElseStatement", nestingLevel);

                // increment nesting for subsequent statements
                nestingLevel += 1;
            }

            // Push the 'else' block back to the stack
            stack.Push((ifStatement.ElseStatement, nestingLevel));
        }
    }

    private static bool IsFlowBreakingStructure(SyntaxNode node)
    {
        // Fast path for common flow-breaking structures
        if (flowBreakingKinds.Contains(node.Kind))
            return true;

        // Apply Cognitive Complexity discount for consecutive logical operators
        var kind = node.Kind;
        if (kind == EnumProvider.SyntaxKind.LogicalAndExpression ||
            kind == EnumProvider.SyntaxKind.LogicalOrExpression ||
            kind == EnumProvider.SyntaxKind.LogicalXorExpression)
            return node.Parent.Kind != node.Kind;

        return false;
    }

    private static bool IsNestedStructure(SyntaxNode node) =>
        nestedStructures.Contains(node.Kind);

    private static bool IsGuardClause(SyntaxNode node)
    {
        return node switch
        {
            // if not <condition> then exit;
            IfStatementSyntax { Statement: ExitStatementSyntax } => true,

            IfStatementSyntax { Statement: ExpressionStatementSyntax { Expression: CodeExpressionSyntax codeExpression } }
                => IsGuardExpression(codeExpression),
            _ => false
        };
    }

    private static bool IsGuardExpression(CodeExpressionSyntax codeExpression)
    {
        return codeExpression switch
        {
            // if not <condition> then continue;
            IdentifierNameSyntax identifier when identifier.GetIdentifierOrLiteralValue() is { } value
                => guardClauseExitCommands.Contains(value),

            InvocationExpressionSyntax invocation => IsGuardInvocation(invocation),
            _ => false
        };
    }

    private static bool IsGuardInvocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => IsGuardCommand(memberAccess),

            // if not <condition> then error;
            IdentifierNameSyntax identifier when identifier.GetIdentifierOrLiteralValue() is { } value
                => guardClauseExitCommands.Contains(value),
            _ => false
        };
    }

    private static bool IsGuardCommand(MemberAccessExpressionSyntax memberAccess)
    {
        if (memberAccess.Expression.GetIdentifierOrLiteralValue() is not { } identifierValue)
            return false;

        // if not <condition> then CurrReport.Break() or .Skip() or .Quit();
        return guardClauseIdentifiers.Contains(identifierValue) &&
               guardClauseExitCommands.Contains(memberAccess.GetNameStringValue() ?? string.Empty);
    }

    #region Recursion

    private int CalculateRecursionComplexity(CodeBlockAnalysisContext context, CognitiveComplexityRecursionGraphService recursion, SyntaxNode root)
    {
        if (recursion is null)
            return 0;

        if (context.OwningSymbol is not IMethodSymbol currentMethod)
            return 0;

        int increment = 0;
        int currentId = currentMethod.Id;

        var visited = new HashSet<int>();

        foreach (var node in root.DescendantNodes())
        {
            SyntaxNode? target = null;

            if (node is InvocationExpressionSyntax)
                target = node;
            else if (node is MemberAccessExpressionSyntax && node.Parent is not InvocationExpressionSyntax)
                target = node;
            else if (node is IdentifierNameSyntax && node.Parent is not InvocationExpressionSyntax && node.Parent is not MemberAccessExpressionSyntax)
                target = node;

            if (target is null)
                continue;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(target, context.CancellationToken);
            if (symbolInfo.Symbol is not IMethodSymbol invokedMethod)
                continue;

            visited.Clear();
            if (IsPathTo(recursion, invokedMethod.Id, currentId, visited))
            {
                increment++;
                RaiseIncrementDiagnostic(context, GetKeywordLocation(target, target.SpanStart), "RecursionCycle", 0);
            }
        }

        return increment;
    }

    private static bool IsPathTo(CognitiveComplexityRecursionGraphService recursion, int fromId, int targetId, HashSet<int> visited)
    {
        if (fromId == targetId)
            return true;

        if (!visited.Add(fromId))
            return false;

        var invokedIds = recursion.GetInvocationIds(fromId);
        if (invokedIds.IsDefaultOrEmpty)
            return false;

        foreach (var nextId in invokedIds)
        {
            if (IsPathTo(recursion, nextId, targetId, visited))
                return true;
        }

        return false;
    }

    #endregion

    private static int LoadCognitiveComplexityThreshold(Compilation compilation)
    {
        var settings = ALCopsSettingsProvider.GetSettings(compilation.FileSystem);

        return settings.CognitiveComplexityThreshold;
    }

    private void RaiseIncrementDiagnostic(CodeBlockAnalysisContext context, Location location, string category, int nestingPenalty)
    {
        if (!this.IsIncrementDiagnosticsEnabled)
            return;

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.CognitiveComplexityIncrement,
                location,
                category,
                nestingPenalty + 1,
                nestingPenalty));
    }

    private static Location GetKeywordLocation(SyntaxNode node, int spanStart)
    {
        return node switch
        {
            IfStatementSyntax ifStatement =>
                ifStatement.IfKeywordToken.GetLocation(),

            CaseStatementSyntax caseStatement =>
                caseStatement.CaseKeywordToken.GetLocation(),

            ForStatementSyntax forStatement =>
                forStatement.ForKeywordToken.GetLocation(),

            ForEachStatementSyntax forEachStatement =>
                forEachStatement.ForEachKeywordToken.GetLocation(),

            WhileStatementSyntax whileStatement =>
                whileStatement.WhileKeywordToken.GetLocation(),

            RepeatStatementSyntax repeatStatement =>
                repeatStatement.RepeatKeywordToken.GetLocation(),

#if NET8_0_OR_GREATER
            ConditionalExpressionSyntax conditionalExpression =>
                conditionalExpression.QuestionToken.GetLocation(),
#endif
            BinaryExpressionSyntax binaryExpression when
                node.IsKind(EnumProvider.SyntaxKind.LogicalAndExpression) ||
                node.IsKind(EnumProvider.SyntaxKind.LogicalOrExpression) ||
                node.IsKind(EnumProvider.SyntaxKind.LogicalXorExpression)
                => binaryExpression.OperatorToken.GetLocation(),

            InvocationExpressionSyntax invocationExpression =>
                invocationExpression.Expression switch
                {
                    IdentifierNameSyntax identifier => identifier.Identifier.GetLocation(),
                    MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.GetLocation(),
                    _ => invocationExpression.GetLocation()
                },

            MemberAccessExpressionSyntax memberAccessExpression =>
                memberAccessExpression.Name.Identifier.GetLocation(),

            IdentifierNameSyntax identifierName =>
                identifierName.Identifier.GetLocation(),

            _ => node.GetLocation().SourceTree!.GetLocation(new TextSpan(spanStart, 1))
        };
    }
}