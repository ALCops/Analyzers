using System.Collections.Concurrent;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.LinterCop.Analyzers;

/// <summary>
/// Simple thread-safe object pool for Stack objects to reduce GC allocations.
/// </summary>
internal sealed class StackPool<T>
{
    private readonly ConcurrentBag<Stack<T>> _pool = new();
    private readonly int _maxPoolSize;

    public StackPool(int maxPoolSize = 16)
    {
        _maxPoolSize = maxPoolSize;
    }

    public Stack<T> Get()
    {
        if (_pool.TryTake(out var stack))
            return stack;

        return new Stack<T>(16); // Pre-allocate reasonable capacity
    }

    public void Return(Stack<T> stack)
    {
        stack.Clear();
        if (_pool.Count < _maxPoolSize)
            _pool.Add(stack);
    }
}

[DiagnosticAnalyzer]
public sealed class CognitiveComplexity : DiagnosticAnalyzer
{
    private sealed class CompilationAnalysisState
    {
        public Compilation Compilation { get; }
        public int ComplexityThreshold { get; }

        // Use string key (fully qualified name) for reliable cross-semantic-model symbol matching
        private readonly ConcurrentDictionary<string, HashSet<IMethodSymbol>> _methodInvocationGraph;
        private readonly ConcurrentDictionary<SyntaxTree, SemanticModel> _semanticModelCache;

        // Index mapping method keys to their syntax nodes for O(1) lookup
        private readonly Lazy<ConcurrentDictionary<string, (SyntaxTree tree, MethodDeclarationSyntax syntax)>> _methodIndex;

        public CompilationAnalysisState(Compilation compilation, int complexityThreshold)
        {
            Compilation = compilation;
            ComplexityThreshold = complexityThreshold;
            _methodInvocationGraph = new();
            _semanticModelCache = new();
            _methodIndex = new Lazy<ConcurrentDictionary<string, (SyntaxTree, MethodDeclarationSyntax)>>(BuildMethodIndex);
        }

        public SemanticModel GetCachedSemanticModel(SyntaxTree tree)
        {
            return _semanticModelCache.GetOrAdd(tree, t => Compilation.GetSemanticModel(t));
        }

        public HashSet<IMethodSymbol> GetMethodInvocations(IMethodSymbol methodSymbol)
        {
            var key = GetMethodKey(methodSymbol);
            return _methodInvocationGraph.GetOrAdd(key, _ => BuildMethodInvocations(key));
        }

        internal static string GetMethodKey(IMethodSymbol methodSymbol)
        {
            // Create a unique key using containing type and method name with parameter types
            var containingType = methodSymbol.ContainingSymbol?.Name ?? string.Empty;
            var parameters = string.Join(",", methodSymbol.Parameters.Select(p => p.ParameterType?.Name ?? "unknown"));
            return $"{containingType}.{methodSymbol.Name}({parameters})";
        }

        /// <summary>
        /// Builds an index of all method declarations for O(1) lookup.
        /// This is lazily initialized on first use and shared across all analyses.
        /// </summary>
        private ConcurrentDictionary<string, (SyntaxTree tree, MethodDeclarationSyntax syntax)> BuildMethodIndex()
        {
            var index = new ConcurrentDictionary<string, (SyntaxTree, MethodDeclarationSyntax)>();

            foreach (var tree in Compilation.SyntaxTrees)
            {
                var root = tree.GetRoot();
                var semanticModel = GetCachedSemanticModel(tree);

                // Use manual iteration instead of LINQ for better performance
                foreach (var node in root.DescendantNodes())
                {
                    if (node is not MethodDeclarationSyntax methodDeclaration)
                        continue;

                    if (methodDeclaration.Body == null || methodDeclaration.Body.Statements.Count == 0)
                        continue;

                    if (semanticModel.GetDeclaredSymbol(methodDeclaration) is IMethodSymbol declaredSymbol)
                    {
                        var key = GetMethodKey(declaredSymbol);
                        index.TryAdd(key, (tree, methodDeclaration));
                    }
                }
            }

            return index;
        }

        private HashSet<IMethodSymbol> BuildMethodInvocations(string methodKey)
        {
            var invokedMethods = new HashSet<IMethodSymbol>();

            // O(1) lookup using the method index
            if (!_methodIndex.Value.TryGetValue(methodKey, out var methodInfo))
                return invokedMethods;

            var (tree, methodDeclaration) = methodInfo;
            var semanticModel = GetCachedSemanticModel(tree);

            // Use manual iteration instead of LINQ OfType<T>() for better performance
            foreach (var node in methodDeclaration.DescendantNodes())
            {
                if (node is not InvocationExpressionSyntax invocation)
                    continue;

                var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                if (symbolInfo.Symbol is IMethodSymbol invokedSymbol)
                {
                    invokedMethods.Add(invokedSymbol);
                }
            }

            return invokedMethods;
        }

        public void Clear()
        {
            _methodInvocationGraph.Clear();
            _semanticModelCache.Clear();
        }
    }

    // Flow-Breaking Structures: These disrupt the linear execution of the code.
    // Each occurrence of these structures adds +1 complexity to the score.
#if NET8_0_OR_GREATER
    private static readonly FrozenSet<SyntaxKind> FlowBreakingKinds = new[]
    {
        EnumProvider.SyntaxKind.IfStatement,
        EnumProvider.SyntaxKind.CaseStatement,
        EnumProvider.SyntaxKind.ForStatement,
        EnumProvider.SyntaxKind.ForEachStatement,
        EnumProvider.SyntaxKind.WhileStatement,
        EnumProvider.SyntaxKind.RepeatStatement,
        EnumProvider.SyntaxKind.ConditionalExpression // Ternary operator
    }.ToFrozenSet();
#else
    private static readonly ImmutableHashSet<SyntaxKind> FlowBreakingKinds = ImmutableHashSet.Create(
        EnumProvider.SyntaxKind.IfStatement,
        EnumProvider.SyntaxKind.CaseStatement,
        EnumProvider.SyntaxKind.ForStatement,
        EnumProvider.SyntaxKind.ForEachStatement,
        EnumProvider.SyntaxKind.WhileStatement,
        EnumProvider.SyntaxKind.RepeatStatement,
        EnumProvider.SyntaxKind.ConditionalExpression // Ternary operator
    );
#endif

    // Nested Structures: These introduce additional cognitive load due to nesting.
    // Unlike flow-breaking structures that always add complexity, nested structures only add an extra penalty when nested inside another structure.
    // Currently there's no difference between the Flow-Breaking Structures and Nested Structures in the AL Language.
    // For example in C# nestedStructures could contain try-catch-finally
#if NET8_0_OR_GREATER
    private static readonly FrozenSet<SyntaxKind> NestedStructures = new[]
    {
        EnumProvider.SyntaxKind.IfStatement,
        EnumProvider.SyntaxKind.CaseStatement,
        EnumProvider.SyntaxKind.ForStatement,
        EnumProvider.SyntaxKind.ForEachStatement,
        EnumProvider.SyntaxKind.WhileStatement,
        EnumProvider.SyntaxKind.RepeatStatement,
        EnumProvider.SyntaxKind.ConditionalExpression // Ternary operator
    }.ToFrozenSet();
#else
    private static readonly ImmutableHashSet<SyntaxKind> NestedStructures = ImmutableHashSet.Create(
        EnumProvider.SyntaxKind.IfStatement,
        EnumProvider.SyntaxKind.CaseStatement,
        EnumProvider.SyntaxKind.ForStatement,
        EnumProvider.SyntaxKind.ForEachStatement,
        EnumProvider.SyntaxKind.WhileStatement,
        EnumProvider.SyntaxKind.RepeatStatement,
        EnumProvider.SyntaxKind.ConditionalExpression // Ternary operator
    );
#endif

    // This HashSet defines specific identifiers that, in certain cases, restrict whether a statement qualifies as a guard clause.
    // Some exit commands (e.g., "Break", "Skip", "Quit") are only considered guard clauses if they are called on these identifiers.
#if NET8_0_OR_GREATER
    private static readonly FrozenSet<string> GuardClauseIdentifiers = new[]
    {
        "CurrReport",
        "CurrXMLport"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
#else
    private static readonly ImmutableHashSet<string> GuardClauseIdentifiers =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "CurrReport", "CurrXMLport");
#endif

    // This HashSet defines commands that act as guard clause exits, meaning they immediately alter the flow of execution.
    // These commands are typically used in scenarios where a function, loop, or process needs to be stopped or skipped under certain conditions.
    // However, "Exit" is not included in this set, as we can get the ExitStatementSyntax type directly on the Statement of the IfStatementSyntax
#if NET8_0_OR_GREATER
    private static readonly FrozenSet<string> GuardClauseExitCommands = new[]
    {
        "Break",
        "Continue",
        "Error",
        "Quit",
        "Skip"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
#else
    private static readonly ImmutableHashSet<string> GuardClauseExitCommands =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "Break", "Continue", "Error", "Quit", "Skip");
#endif

#if NET8_0_OR_GREATER
    private static readonly FrozenSet<string> EventPublisherDecoratorNames = new[]
    {
        "BusinessEvent",
        "IntegrationEvent",
        "ExternalBusinessEvent"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
#else
    private static readonly ImmutableHashSet<string> EventPublisherDecoratorNames =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "BusinessEvent", "IntegrationEvent", "ExternalBusinessEvent");
#endif

    // Object pool for stack reuse to reduce GC pressure in CalculateCognitiveComplexity
    private static readonly StackPool<(SyntaxNode node, int nestingLevel)> TraversalStackPool = new();

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.CognitiveComplexityMetric,
            DiagnosticDescriptors.CognitiveComplexityIncrement,
            DiagnosticDescriptors.CognitiveComplexityThresholdExceeded
        );

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var state = new CompilationAnalysisState(
                compilationContext.Compilation,
                LoadCognitiveComplexityThreshold(compilationContext.Compilation));

            compilationContext.RegisterCodeBlockAction(codeBlockContext =>
            {
                AnalyzeCognitiveComplexity(codeBlockContext, state);
            });

            // Clean up resources when compilation ends
            compilationContext.RegisterCompilationEndAction(_ => state.Clear());
        });
    }

    private void AnalyzeCognitiveComplexity(CodeBlockAnalysisContext context, CompilationAnalysisState state)
    {
        if (context.IsObsolete() || context.CodeBlock is not MethodOrTriggerDeclarationSyntax methodOrTrigger)
            return;

        var containingObjectTypeSymbol = context.OwningSymbol.GetContainingObjectTypeSymbol();
        if (containingObjectTypeSymbol.NavTypeKind == EnumProvider.NavTypeKind.Interface ||
            containingObjectTypeSymbol.NavTypeKind == EnumProvider.NavTypeKind.ControlAddIn)
            return;

        if (methodOrTrigger.Body is null ||
            methodOrTrigger.Body.Statements.Count == 0 &&
            methodOrTrigger.Attributes.Any(attr => EventPublisherDecoratorNames.Contains(attr.GetIdentifierOrLiteralValue() ?? string.Empty)))
            return;

        int complexity = CalculateCognitiveComplexity(context, state, methodOrTrigger.Body);
        if (complexity >= state.ComplexityThreshold)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.CognitiveComplexityThresholdExceeded,
                context.OwningSymbol.GetLocation(),
                complexity,
                state.ComplexityThreshold));
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.CognitiveComplexityMetric,
            context.OwningSymbol.GetLocation(),
            complexity,
            state.ComplexityThreshold));
    }

    private int CalculateCognitiveComplexity(CodeBlockAnalysisContext context, CompilationAnalysisState state, SyntaxNode root)
    {
        int complexity = 0;
        var stack = TraversalStackPool.Get();

        try
        {
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
                var detector = new RecursionDetector(state);
                complexity += detector.CalculateComplexity(
                    context,
                    root,
                    location => RaiseIncrementDiagnostic(context, location, "RecursionCycle", 0),
                    context.CancellationToken);
            }

            return complexity;
        }
        finally
        {
            TraversalStackPool.Return(stack);
        }
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
        if (FlowBreakingKinds.Contains(node.Kind))
            return true;

        var kind = node.Kind;

        // Apply Cognitive Complexity discount for consecutive logical operators
        if (kind == EnumProvider.SyntaxKind.LogicalAndExpression ||
            kind == EnumProvider.SyntaxKind.LogicalOrExpression ||
            kind == EnumProvider.SyntaxKind.LogicalXorExpression)
        {
            return node.Parent?.Kind != kind;
        }

        return false;
    }

    private static bool IsNestedStructure(SyntaxNode node) =>
        NestedStructures.Contains(node.Kind);

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
                => GuardClauseExitCommands.Contains(value),

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
                => GuardClauseExitCommands.Contains(value),
            _ => false
        };
    }

    private static bool IsGuardCommand(MemberAccessExpressionSyntax memberAccess)
    {
        if (memberAccess.Expression.GetIdentifierOrLiteralValue() is not { } identifierValue)
            return false;

        // if not <condition> then CurrReport.Break() or .Skip() or .Quit();
        return GuardClauseIdentifiers.Contains(identifierValue) &&
               GuardClauseExitCommands.Contains(memberAccess.GetNameStringValue() ?? string.Empty);
    }

    #region Recursion

    /// <summary>
    /// Detects direct and indirect recursion cycles in method call graphs.
    /// Encapsulates recursion detection logic for better separation of concerns.
    /// </summary>
    private sealed class RecursionDetector
    {
        private readonly CompilationAnalysisState _state;
        private readonly HashSet<string> _visited = new();

        public RecursionDetector(CompilationAnalysisState state)
        {
            _state = state;
        }

        /// <summary>
        /// Calculates recursion complexity by detecting cycles from invocations back to the current method.
        /// </summary>
        public int CalculateComplexity(
            CodeBlockAnalysisContext context,
            SyntaxNode root,
            Action<Location> onRecursionFound,
            CancellationToken cancellationToken)
        {
            int increment = 0;

            if (context.OwningSymbol is not IMethodSymbol currentMethod)
                return increment;

            var currentMethodKey = CompilationAnalysisState.GetMethodKey(currentMethod);

            // Use manual iteration instead of LINQ OfType<T>() for better performance
            foreach (var node in root.DescendantNodes())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (node is not InvocationExpressionSyntax invocation)
                    continue;

                var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, cancellationToken);
                if (symbolInfo.Symbol is IMethodSymbol invokedMethod)
                {
                    // Check if there is a path from the invoked method back to the current method.
                    _visited.Clear();
                    if (HasPathTo(invokedMethod, currentMethodKey, cancellationToken))
                    {
                        increment++;
                        onRecursionFound(GetKeywordLocation(invocation, invocation.SpanStart));
                    }
                }
            }

            return increment;
        }

        /// <summary>
        /// Checks if there is a path from the given method to the target method key (detecting cycles).
        /// </summary>
        private bool HasPathTo(IMethodSymbol from, string targetKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fromKey = CompilationAnalysisState.GetMethodKey(from);

            if (string.Equals(fromKey, targetKey, StringComparison.Ordinal))
                return true;

            if (!_visited.Add(fromKey))
                return false;

            var invokedMethods = _state.GetMethodInvocations(from);

            foreach (var invokedMethod in invokedMethods)
            {
                if (HasPathTo(invokedMethod, targetKey, cancellationToken))
                    return true;
            }

            return false;
        }
    }

    #endregion

    private static int LoadCognitiveComplexityThreshold(Compilation compilation)
    {
        var settings = ALCopsSettingsProvider.GetSettings(
            compilation.FileSystem?.GetDirectoryPath());

        return settings.CognitiveComplexityThreshold;
    }

    private static void RaiseIncrementDiagnostic(CodeBlockAnalysisContext context, Location location, string category, int nestingPenalty)
    {
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

            _ => node.GetLocation().SourceTree!.GetLocation(new TextSpan(spanStart, 1))
        };
    }
}