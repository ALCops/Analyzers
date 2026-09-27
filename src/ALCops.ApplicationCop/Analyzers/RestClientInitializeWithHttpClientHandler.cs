using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class RestClientInitializeWithHttpClientHandler : DiagnosticAnalyzer
{
    private const int RestClientId = 2350;
    private const string RestClientName = "Rest Client";
    private const int DefaultHandlerId = 2360;
    private const string HandlerName = "Http Client Handler";

    // Member kinds whose own members can declare code (triggers, nested controls, actions, dataitems, nodes).
    // Keys are left out on purpose: in a tableextension they enumerate the target table's fields, which would
    // attribute base-table field triggers to the extension.
    private static readonly ImmutableHashSet<SymbolKind> DescendKinds = ImmutableHashSet.Create(
        EnumProvider.SymbolKind.Field,
        EnumProvider.SymbolKind.Control,
        EnumProvider.SymbolKind.Action,
        EnumProvider.SymbolKind.Change,
        EnumProvider.SymbolKind.ReportDataItem,
        EnumProvider.SymbolKind.XmlPortNode,
        EnumProvider.SymbolKind.RequestPage,
        EnumProvider.SymbolKind.RequestPageExtension);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.RestClientInitializeWithHttpClientHandler);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeObject,
            EnumProvider.SymbolKind.Codeunit,
            EnumProvider.SymbolKind.Table,
            EnumProvider.SymbolKind.TableExtension,
            EnumProvider.SymbolKind.Page,
            EnumProvider.SymbolKind.PageExtension,
            EnumProvider.SymbolKind.Report,
            EnumProvider.SymbolKind.ReportExtension,
            EnumProvider.SymbolKind.Query,
            EnumProvider.SymbolKind.XmlPort);

    private static void AnalyzeObject(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Symbol is not IContainerSymbol container)
            return;

        if (ctx.Symbol is ICodeunitTypeSymbol codeunit &&
            (codeunit.Subtype == EnumProvider.CodeunitSubtypeKind.Test ||
             codeunit.Subtype == EnumProvider.CodeunitSubtypeKind.TestRunner))
            return;

        CancellationToken ct = ctx.CancellationToken;

        // Roots are found on symbols only: the declaration may spell the type as `Codeunit 2350`.
        var methods = new List<IMethodSymbol>();
        CollectMethods(container, methods, ct);

        var globalRoots = container.GetMembers().OfType<IVariableSymbol>().Where(v => IsRestClient(v.Type)).ToList();
        bool hasLocalRoot = methods.Exists(m => m.LocalVariables.Any(v => IsRestClient(v.Type)));
        if (globalRoots.Count == 0 && !hasLocalRoot)
            return;

        if (ctx.Symbol.DeclaringSyntaxReference?.GetSyntax(ct) is not ObjectSyntax objectSyntax)
            return;

        SemanticModel model = ctx.Compilation.GetSemanticModel(objectSyntax.SyntaxTree);

        foreach (IMethodSymbol method in methods)
        {
            if (method.IsObsolete())
                continue;

            foreach (IVariableSymbol local in method.LocalVariables)
            {
                ct.ThrowIfCancellationRequested();

                if (!IsRestClient(local.Type) || GetBody(method, ct) is not { } body)
                    continue;

                var state = new RootState();
                Walk(local, body, state, model, objectSyntax, ct);
                Report(ctx, local, state);
            }
        }

        foreach (IVariableSymbol global in globalRoots)
        {
            var state = new RootState();
            foreach (IMethodSymbol method in methods)
            {
                ct.ThrowIfCancellationRequested();

                if (state.Unknown)
                    break;

                // Sound pre-filter: every reference to the global spells its name in the body.
                if (GetBody(method, ct) is not { } body ||
                    body.ToString().IndexOf(global.Name, SemanticFacts.NameEqualityComparison) < 0)
                    continue;

                Walk(global, body, state, model, objectSyntax, ct);
            }

            Report(ctx, global, state);
        }
    }

    private static void Walk(ISymbol root, BlockSyntax body, RootState state, SemanticModel model, ObjectSyntax objectSyntax, CancellationToken ct)
    {
        if (model.GetOperation(body, ct) is { } operation)
            new TrackedWalker(root, true, state, model, objectSyntax, new HashSet<IMethodSymbol>(), ct).Visit(operation);
    }

    private static void Report(SymbolAnalysisContext ctx, IVariableSymbol root, RootState state)
    {
        if (state.Unknown)
            return;

        Location? location = null;
        if (state.Default)
            location = state.DefaultLocation ?? root.GetLocation();
        else if (state.Used && !state.Good)
            location = root.GetLocation();

        if (location is not null)
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RestClientInitializeWithHttpClientHandler, location, root.Name));
    }

    private static void CollectMethods(IContainerSymbol container, List<IMethodSymbol> methods, CancellationToken ct)
    {
        foreach (ISymbol member in container.GetMembers())
        {
            ct.ThrowIfCancellationRequested();

            if (member is IMethodSymbol method)
                methods.Add(method);
            else if (member is IContainerSymbol nested && DescendKinds.Contains(member.Kind))
                CollectMethods(nested, methods, ct);
        }
    }

    private static BlockSyntax? GetBody(IMethodSymbol method, CancellationToken ct) =>
        (method.DeclaringSyntaxReference?.GetSyntax(ct) as MethodOrTriggerDeclarationSyntax)?.Body;

    private static bool IsRestClient(ITypeSymbol? type) => IsCodeunit(type, RestClientId, RestClientName);

    private static bool IsCodeunit(ITypeSymbol? type, int id, string name) =>
        type is not null &&
        type.NavTypeKind == EnumProvider.NavTypeKind.Codeunit &&
        (type.OriginalDefinition as ICodeunitTypeSymbol ?? type as ICodeunitTypeSymbol) is { } codeunit &&
        codeunit.Id == id &&
        SemanticFacts.IsSameName(codeunit.Name, name);

    private sealed class RootState
    {
        public bool Used { get; set; }
        public bool Unknown { get; set; }
        public bool Good { get; set; }
        public bool Default { get; set; }
        public Location? DefaultLocation { get; set; }
    }

    // Tracks one Rest Client variable (the root, or the parameter it was passed into) through a body.
    // Anything the walker cannot follow makes the root unknown, which silences it.
    private sealed class TrackedWalker(
        ISymbol tracked,
        bool direct,
        RootState state,
        SemanticModel model,
        ObjectSyntax objectSyntax,
        HashSet<IMethodSymbol> visited,
        CancellationToken ct) : OperationWalker
    {
        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            if (state.Unknown)
                return;
            ct.ThrowIfCancellationRequested();

            if (IsTracked(operation.Instance))
            {
                if (operation.IsInvalid)
                {
                    state.Unknown = true;
                    return;
                }

                // A discarded `RestClient.Create(Handler);` also initializes the receiver itself.
                if (SemanticFacts.IsSameName(operation.TargetMethod.Name, "Initialize") ||
                    SemanticFacts.IsSameName(operation.TargetMethod.Name, "Create"))
                    ClassifyInitialization(operation);
                else
                    state.Used = true;
            }

            for (int i = 0; i < operation.Arguments.Length; i++)
            {
                if (IsTracked(operation.Arguments[i].Value) && !TryFollow(operation.TargetMethod, i))
                {
                    state.Unknown = true;
                    return;
                }
            }

            base.VisitInvocationExpression(operation);
        }

        public override void VisitAssignmentStatement(IAssignmentStatement operation)
        {
            if (state.Unknown)
                return;

            if (IsTracked(operation.Target))
            {
                // Only `RestClient := RestClient.Create(...)` keeps the instance known.
                if (operation.Value.UnwrapConversions() is not IInvocationExpression invocation ||
                    !IsTracked(invocation.Instance) ||
                    !SemanticFacts.IsSameName(invocation.TargetMethod.Name, "Create"))
                {
                    state.Unknown = true;
                    return;
                }
            }
            else if (IsTracked(operation.Value))
            {
                state.Unknown = true;
                return;
            }

            base.VisitAssignmentStatement(operation);
        }

        public override void VisitExitStatement(IExitStatement operation)
        {
            if (state.Unknown)
                return;

            if (IsTracked(operation.ReturnedValue))
            {
                state.Unknown = true;
                return;
            }

            base.VisitExitStatement(operation);
        }

        private void ClassifyInitialization(IInvocationExpression operation)
        {
            // IArgument.Parameter is null when binding failed.
            IArgument? handlerArgument = operation.Arguments.FirstOrDefault(a =>
                a.Parameter?.ParameterType is { } type &&
                type.NavTypeKind == EnumProvider.NavTypeKind.Interface &&
                SemanticFacts.IsSameName(type.Name, HandlerName));

            if (handlerArgument is not null &&
                !IsCodeunit(handlerArgument.Value.UnwrapConversions().Type, DefaultHandlerId, HandlerName))
            {
                state.Good = true;
                return;
            }

            state.Default = true;
            if (direct && state.DefaultLocation is null)
                state.DefaultLocation = operation.Syntax.GetLocation();
        }

        // Follows the tracked variable into a procedure declared inside the same object syntax. Syntax
        // containment also covers request-page procedures, whose containing application object is null.
        private bool TryFollow(IMethodSymbol target, int argumentIndex)
        {
            if (target.MethodKind != EnumProvider.MethodKind.Method ||
                target.IsEvent ||
                argumentIndex >= target.Parameters.Length ||
                target.DeclaringSyntaxReference?.GetSyntax(ct) is not MethodOrTriggerDeclarationSyntax { Body: { } body } callee ||
                callee.SyntaxTree != objectSyntax.SyntaxTree ||
                !objectSyntax.Span.Contains(callee.Span))
                return false;

            if (!visited.Add(target))
                return true;

            try
            {
                if (model.GetOperation(body, ct) is not { } operation)
                    return false;

                new TrackedWalker(target.Parameters[argumentIndex], false, state, model, objectSyntax, visited, ct).Visit(operation);
                return true;
            }
            finally
            {
                visited.Remove(target);
            }
        }

        // The SDK has no SymbolEqualityComparer; ISymbol.Equals compares the declared symbols.
        private bool IsTracked(IOperation? operation) =>
            operation is not null &&
            operation.UnwrapConversions().GetSymbolSafe() is { } symbol &&
            symbol.Equals(tracked);
    }
}
