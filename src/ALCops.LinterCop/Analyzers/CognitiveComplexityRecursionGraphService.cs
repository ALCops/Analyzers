using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.LinterCop.Analyzers;

internal sealed class CognitiveComplexityRecursionGraphService
{
    private readonly Compilation _compilation;
    private readonly ConcurrentDictionary<SyntaxTree, SemanticModel> _semanticModels = new();
    private readonly ConcurrentDictionary<int, Lazy<ImmutableArray<int>>> _invocationIds = new();
    private readonly Lazy<ImmutableDictionary<int, MethodDeclarationInfo>> _methodDeclarationInfo;

    public CognitiveComplexityRecursionGraphService(Compilation compilation)
    {
        _compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
        _methodDeclarationInfo =
            new Lazy<ImmutableDictionary<int, MethodDeclarationInfo>>(
                BuildMethodDeclarationInfoIndex,
                LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public ImmutableArray<int> GetInvocationIds(IMethodSymbol methodSymbol)
    {
        if (methodSymbol is null)
            throw new ArgumentNullException(nameof(methodSymbol));

        return GetInvocationIds(methodSymbol.Id);
    }

    public ImmutableArray<int> GetInvocationIds(int methodId)
    {
        var lazy = _invocationIds.GetOrAdd(
            methodId,
            id => new Lazy<ImmutableArray<int>>(
                () => BuildInvocationIds(id),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return lazy.Value;
    }

    private SemanticModel GetCachedSemanticModel(SyntaxTree tree)
        => _semanticModels.GetOrAdd(tree, t => _compilation.GetSemanticModel(t));

    private ImmutableArray<int> BuildInvocationIds(int methodId)
    {
        if (!_methodDeclarationInfo.Value.TryGetValue(methodId, out var methodInfo))
            return ImmutableArray<int>.Empty;

        var body = methodInfo.Declaration.Body;
        if (body is null || body.Statements.Count == 0)
            return ImmutableArray<int>.Empty;

        var semanticModel = GetCachedSemanticModel(methodInfo.Tree);

        var builder = ImmutableArray.CreateBuilder<int>();

        foreach (var node in body.DescendantNodes())
        {
            if (node is not InvocationExpressionSyntax invocation)
                continue;

            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol invokedSymbol)
            {
                builder.Add(invokedSymbol.Id);
            }
        }

        return builder.Count == 0 ? ImmutableArray<int>.Empty : builder.ToImmutable();
    }

    private ImmutableDictionary<int, MethodDeclarationInfo> BuildMethodDeclarationInfoIndex()
    {
        var builder = ImmutableDictionary.CreateBuilder<int, MethodDeclarationInfo>();

        foreach (var tree in _compilation.SyntaxTrees)
        {
            var root = tree.GetRoot();
            var semanticModel = GetCachedSemanticModel(tree);

            foreach (var node in root.DescendantNodes())
            {
                if (node is not MethodDeclarationSyntax methodDeclaration)
                    continue;

                var body = methodDeclaration.Body;
                if (body is null || body.Statements.Count == 0)
                    continue;

                if (semanticModel.GetDeclaredSymbol(methodDeclaration) is not IMethodSymbol methodSymbol)
                    continue;

                builder[methodSymbol.Id] = new MethodDeclarationInfo(tree, methodDeclaration);
            }
        }

        return builder.ToImmutable();
    }

#if NETSTANDARD2_1
    private readonly struct MethodDeclarationInfo
    {
        public SyntaxTree Tree { get; }
        public MethodDeclarationSyntax Declaration { get; }

        public MethodDeclarationInfo(SyntaxTree tree, MethodDeclarationSyntax declaration)
        {
            Tree = tree;
            Declaration = declaration;
        }
    }
#else
    private readonly record struct MethodDeclarationInfo(SyntaxTree Tree, MethodDeclarationSyntax Declaration);
#endif
}
