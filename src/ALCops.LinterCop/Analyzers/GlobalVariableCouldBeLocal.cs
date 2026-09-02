using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class GlobalVariableCouldBeLocal : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.GlobalVariableCouldBeLocal);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCompilationAction(AnalyzeCompilation);

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        foreach (var applicationObject in context.Compilation.GetDeclaredApplicationObjectSymbols())
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            AnalyzeApplicationObject(context, applicationObject);
        }
    }

    private static void AnalyzeApplicationObject(
        CompilationAnalysisContext context,
        IApplicationObjectTypeSymbol applicationObject)
    {
        if (applicationObject.IsObsolete())
        {
            return;
        }

        if (applicationObject is not ICodeunitTypeSymbol)
        {
            return;
        }

        if (HasUnsupportedExecutionModel(applicationObject))
        {
            return;
        }

        if (ExposesObjectStateThroughEventPublisher(applicationObject))
        {
            return;
        }

        var candidates = applicationObject.GetMembers()
            .OfType<IVariableSymbol>()
            .Where(IsCandidate)
            .ToArray();

        if (candidates.Length == 0)
        {
            return;
        }

        var objectSyntax = GetApplicationObjectSyntax(applicationObject, candidates, context.CancellationToken);
        if (objectSyntax is null)
        {
            return;
        }

        var semanticModel = context.Compilation.GetSemanticModel(objectSyntax.SyntaxTree);
        var usages = CollectUsages(objectSyntax, semanticModel, candidates, context.CancellationToken);
        var groups = new Dictionary<int, MethodGroup>();

        foreach (var candidate in candidates)
        {
            if (!usages.TryGetValue(candidate.Name, out var usage) ||
                usage.IsInvalid ||
                usage.Scope is null ||
                usage.HasMultipleScopes)
            {
                continue;
            }

            if (semanticModel.GetDeclaredSymbol(usage.Scope, context.CancellationToken) is not IMethodSymbol method ||
                method.IsObsolete())
            {
                continue;
            }

            if (!groups.TryGetValue(method.Id, out var group))
            {
                group = new MethodGroup(method, usage.Scope);
                groups.Add(method.Id, group);
            }

            group.Candidates.Add(candidate);
        }

        foreach (var group in groups.Values)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var labels = group.Candidates
                .Where(IsLabel)
                .ToArray();

            foreach (var label in labels)
            {
                Report(context, label, group.Method);
            }

            var mutableCandidates = group.Candidates
                .Where(candidate => !IsLabel(candidate))
                .ToArray();

            if (mutableCandidates.Length == 0 || group.Declaration.Body is null)
            {
                continue;
            }

            var operation = semanticModel.GetOperation(group.Declaration.Body, context.CancellationToken);
            if (operation is null || operation.IsInvalid)
            {
                continue;
            }

            var walker = new DefiniteInitializationWalker(mutableCandidates, context.CancellationToken);
            walker.Visit(operation);

            foreach (var candidate in mutableCandidates)
            {
                if (walker.IsSafe(candidate))
                {
                    Report(context, candidate, group.Method);
                }
            }
        }
    }

    private static bool IsCandidate(IVariableSymbol variable) =>
        variable.Kind == EnumProvider.SymbolKind.GlobalVariable &&
        !variable.IsSynthesized &&
        !variable.IsObsolete() &&
        variable.DeclaredAccessibility != EnumProvider.Accessibility.Protected &&
        variable.DeclaringSyntaxReference is not null &&
        HasSupportedValueSemantics(variable.Type);

    private static bool HasSupportedValueSemantics(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol or ITextConstTypeSymbol)
        {
            return false;
        }

        if (type is IRecordTypeSymbol recordType)
        {
            return !recordType.IsTemporary() &&
                recordType.OriginalDefinition is ITableTypeSymbol tableType &&
                tableType.TableType == EnumProvider.TableTypeKind.Normal;
        }

        var typeKind = type.GetNavTypeKindSafe();
        return typeKind == EnumProvider.NavTypeKind.BigInteger ||
            typeKind == EnumProvider.NavTypeKind.Boolean ||
            typeKind == EnumProvider.NavTypeKind.Byte ||
            typeKind == EnumProvider.NavTypeKind.Char ||
            typeKind == EnumProvider.NavTypeKind.Code ||
            typeKind == EnumProvider.NavTypeKind.Date ||
            typeKind == EnumProvider.NavTypeKind.DateFormula ||
            typeKind == EnumProvider.NavTypeKind.DateTime ||
            typeKind == EnumProvider.NavTypeKind.Decimal ||
            typeKind == EnumProvider.NavTypeKind.Duration ||
            typeKind == EnumProvider.NavTypeKind.Enum ||
            typeKind == EnumProvider.NavTypeKind.Guid ||
            typeKind == EnumProvider.NavTypeKind.Integer ||
            typeKind == EnumProvider.NavTypeKind.Label ||
            typeKind == EnumProvider.NavTypeKind.Option ||
            typeKind == EnumProvider.NavTypeKind.RecordId ||
            typeKind == EnumProvider.NavTypeKind.Text ||
            typeKind == EnumProvider.NavTypeKind.Time;
    }

    private static bool IsLabel(IVariableSymbol variable) =>
        variable.Type.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Label;

    private static bool ExposesObjectStateThroughEventPublisher(
        IApplicationObjectTypeSymbol applicationObject) =>
        applicationObject.GetMembers()
            .OfType<IMethodSymbol>()
            .SelectMany(method => method.Attributes)
            .Any(attribute =>
                ((attribute.AttributeKind == EnumProvider.AttributeKind.IntegrationEvent ||
                  attribute.AttributeKind == EnumProvider.AttributeKind.BusinessEvent ||
                  attribute.AttributeKind == EnumProvider.AttributeKind.InternalEvent) &&
                 attribute.Arguments.Length > 0 &&
                 bool.TryParse(attribute.Arguments[0].ValueText, out var includeSender) &&
                 includeSender) ||
                (attribute.AttributeKind == EnumProvider.AttributeKind.IntegrationEvent &&
                 attribute.Arguments.Length > 1 &&
                 bool.TryParse(attribute.Arguments[1].ValueText, out var globalVarAccess) &&
                 globalVarAccess));

    private static ApplicationObjectSyntax? GetApplicationObjectSyntax(
        IApplicationObjectTypeSymbol applicationObject,
        IVariableSymbol[] candidates,
        CancellationToken cancellationToken)
    {
        if (applicationObject.DeclaringSyntaxReference?.GetSyntax(cancellationToken) is ApplicationObjectSyntax objectSyntax)
        {
            return objectSyntax;
        }

        var syntax = candidates[0].DeclaringSyntaxReference?.GetSyntax(cancellationToken);
        while (syntax is not null)
        {
            if (syntax is ApplicationObjectSyntax containingObject)
            {
                return containingObject;
            }

            syntax = syntax.Parent;
        }

        return null;
    }

    private static Dictionary<string, VariableUsage> CollectUsages(
        ApplicationObjectSyntax objectSyntax,
        SemanticModel semanticModel,
        IEnumerable<IVariableSymbol> candidates,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, VariableUsage>(SemanticFacts.NameEqualityComparer);

        foreach (var candidate in candidates)
        {
            result[candidate.Name] = new VariableUsage(candidate);
        }

        foreach (var identifier in objectSyntax.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var name = identifier.Identifier.ValueText;
            if (name is null || !result.TryGetValue(name, out var usage) || IsInsideVariableDeclaration(identifier))
            {
                continue;
            }

            var symbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol;
            if (symbol is null)
            {
                usage.IsInvalid = true;
                continue;
            }

            if (!IsSameVariable(symbol, usage.Variable))
            {
                continue;
            }

            var scope = GetContainingMethodOrTrigger(identifier);
            if (scope is null)
            {
                usage.IsInvalid = true;
                continue;
            }

            if (usage.Scope is null)
            {
                usage.Scope = scope;
            }
            else if (usage.Scope.SpanStart != scope.SpanStart)
            {
                usage.HasMultipleScopes = true;
            }
        }

        return result;
    }

    private static bool IsInsideVariableDeclaration(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is VariableDeclarationSyntax)
            {
                return true;
            }

            if (current is MethodOrTriggerDeclarationSyntax || current is ApplicationObjectSyntax)
            {
                return false;
            }
        }

        return false;
    }

    private static MethodOrTriggerDeclarationSyntax? GetContainingMethodOrTrigger(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is MethodOrTriggerDeclarationSyntax methodOrTrigger)
            {
                return methodOrTrigger;
            }

            if (current is ApplicationObjectSyntax)
            {
                return null;
            }
        }

        return null;
    }

    private static bool HasUnsupportedExecutionModel(IApplicationObjectTypeSymbol applicationObject)
    {
        if (applicationObject is not ICodeunitTypeSymbol codeunit)
        {
            return false;
        }

        if (codeunit.Subtype != EnumProvider.CodeunitSubtypeKind.Normal)
        {
            return true;
        }

        if (codeunit.GetBooleanPropertyValue(EnumProvider.PropertyKind.SingleInstance) is true)
        {
            return true;
        }

        return codeunit.GetEnumPropertyValue<EventSubscriberInstanceKind>(
            EnumProvider.PropertyKind.EventSubscriberInstance) == EnumProvider.EventSubscriberInstanceKind.Manual;
    }

    private static void Report(
        CompilationAnalysisContext context,
        IVariableSymbol variable,
        IMethodSymbol method) =>
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.GlobalVariableCouldBeLocal,
            variable.GetLocation(),
            variable.Name,
            method.Name));

    private static bool IsSameVariable(ISymbol? symbol, IVariableSymbol candidate)
    {
        if (symbol is not IVariableSymbol variable)
        {
            return false;
        }

        return variable.Equals(candidate) || variable.OriginalDefinition.Equals(candidate.OriginalDefinition);
    }

    private sealed class VariableUsage(IVariableSymbol variable)
    {
        public IVariableSymbol Variable { get; } = variable;
        public MethodOrTriggerDeclarationSyntax? Scope { get; set; }
        public bool HasMultipleScopes { get; set; }
        public bool IsInvalid { get; set; }
    }

    private sealed class MethodGroup(IMethodSymbol method, MethodOrTriggerDeclarationSyntax declaration)
    {
        public IMethodSymbol Method { get; } = method;
        public MethodOrTriggerDeclarationSyntax Declaration { get; } = declaration;
        public List<IVariableSymbol> Candidates { get; } = [];
    }

    private enum InitializationKind
    {
        Unknown,
        RecordFieldsInitialized,
        FullyInitialized
    }

    private sealed class CandidateState
    {
        public InitializationKind Initialization { get; set; }
        public bool IsReachable { get; set; } = true;
        public bool IsUnsafe { get; set; }
        public bool ReliesOnGetFieldInitialization { get; set; }
        public bool HasPersistentRecordStateMutation { get; set; }

        public CandidateState Clone() => new()
        {
            Initialization = Initialization,
            IsReachable = IsReachable,
            IsUnsafe = IsUnsafe,
            ReliesOnGetFieldInitialization = ReliesOnGetFieldInitialization,
            HasPersistentRecordStateMutation = HasPersistentRecordStateMutation
        };
    }

    private sealed class DefiniteInitializationWalker : OperationWalker
    {
        private readonly Dictionary<string, IVariableSymbol> _candidates;
        private readonly CancellationToken _cancellationToken;
        private Dictionary<string, CandidateState> _states;
        private bool _standaloneInvocation;

        public DefiniteInitializationWalker(
            IEnumerable<IVariableSymbol> candidates,
            CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _candidates = new Dictionary<string, IVariableSymbol>(SemanticFacts.NameEqualityComparer);
            _states = new Dictionary<string, CandidateState>(SemanticFacts.NameEqualityComparer);

            foreach (var candidate in candidates)
            {
                _candidates.Add(candidate.Name, candidate);
                _states.Add(candidate.Name, new CandidateState());
            }
        }

        public bool IsSafe(IVariableSymbol candidate) =>
            _states.TryGetValue(candidate.Name, out var state) &&
            !state.IsUnsafe &&
            !(state.ReliesOnGetFieldInitialization && state.HasPersistentRecordStateMutation);

        public override void Visit(IOperation operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (EnumProvider.OperationKind.ContinueStatement is { } continueStatement &&
                operation.Kind == continueStatement)
            {
                MarkAllCandidatesUnsafe();
                return;
            }

            if (operation is IGlobalReferenceExpression globalReference &&
                TryGetCandidate(globalReference.GlobalVariable, out var candidate, out var state))
            {
                Require(state, InitializationKind.FullyInitialized);
                return;
            }

            base.Visit(operation);
        }

        public override void VisitExpressionStatement(IExpressionStatement operation)
        {
            if (operation.Expression is IInvocationExpression invocation)
            {
                var previous = _standaloneInvocation;
                _standaloneInvocation = true;
                VisitInvocationExpression(invocation);
                _standaloneInvocation = previous;
                return;
            }

            Visit(operation.Expression);
        }

        public override void VisitAssignmentStatement(IAssignmentStatement operation)
        {
            if (TryGetDirectCandidate(operation.Target, out var candidate, out var state))
            {
                Visit(operation.Value);

                if (state.IsReachable)
                {
                    var isRecord = candidate.Type.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Record;
                    if (isRecord)
                    {
                        state.HasPersistentRecordStateMutation = true;
                    }

                    state.Initialization = isRecord
                        ? InitializationKind.RecordFieldsInitialized
                        : InitializationKind.FullyInitialized;
                }

                return;
            }

            if (ContainsCandidate(operation.Target))
            {
                MarkReferencedCandidatesUnsafe(operation.Target);
            }

            Visit(operation.Target);
            Visit(operation.Value);
        }

        public override void VisitCompoundAssignmentStatement(ICompoundAssignmentStatement operation)
        {
            if (TryGetDirectCandidate(operation.Target, out var candidate, out var state))
            {
                Require(state, InitializationKind.FullyInitialized);
                Visit(operation.Value);
                return;
            }

            if (ContainsCandidate(operation.Target))
            {
                MarkReferencedCandidatesUnsafe(operation.Target);
                Visit(operation.Target);
                Visit(operation.Value);
                return;
            }

            Visit(operation.Target);
            Visit(operation.Value);
        }

        public override void VisitFieldAccess(IFieldAccess operation)
        {
            if (TryGetDirectCandidate(operation.Instance, out var candidate, out var state))
            {
                var requiredState = operation.FieldSymbol.FieldClass == EnumProvider.FieldClassKind.Normal
                    ? InitializationKind.RecordFieldsInitialized
                    : InitializationKind.FullyInitialized;
                Require(state, requiredState);
                return;
            }

            base.VisitFieldAccess(operation);
        }

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            var resultIsDiscarded = _standaloneInvocation;
            _standaloneInvocation = false;

            try
            {
                if (TryHandleClear(operation) ||
                    TryHandleClearAll(operation) ||
                    TryHandleRecordGet(operation, resultIsDiscarded))
                {
                    return;
                }

                if (TryGetDirectCandidate(operation.Instance, out var receiver, out var receiverState))
                {
                    if (receiver.Type.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Record)
                    {
                        receiverState.HasPersistentRecordStateMutation = true;
                        receiverState.IsUnsafe = true;
                    }

                    Require(receiverState, InitializationKind.FullyInitialized);
                }
                else if (operation.Instance is not null)
                {
                    Visit(operation.Instance);
                }

                foreach (var argument in operation.Arguments)
                {
                    if (argument.Parameter?.IsVar == true && ContainsCandidate(argument.Value))
                    {
                        MarkReferencedCandidatesUnsafe(argument.Value);
                    }

                    Visit(argument.Value);
                }

                if (IsFlowTerminatingCall(operation))
                {
                    foreach (var state in _states.Values)
                    {
                        state.IsReachable = false;
                    }

                    return;
                }

                InvalidateInitializedStateForPotentialReentrancy();
            }
            finally
            {
                _standaloneInvocation = resultIsDiscarded;
            }
        }

        public override void VisitIfStatement(IIfStatement operation)
        {
            if (TryVisitConditionalRecordGet(operation.Condition, out var getCandidate, out var successOnTrueBranch))
            {
                var beforeGetBranches = CloneStates();
                var trueSeed = CloneStateDictionary(beforeGetBranches);
                var falseSeed = CloneStateDictionary(beforeGetBranches);
                var successSeed = successOnTrueBranch ? trueSeed : falseSeed;
                var successState = successSeed[getCandidate.Name];

                if (successState.IsReachable && !successState.IsUnsafe &&
                    successState.Initialization < InitializationKind.RecordFieldsInitialized)
                {
                    successState.ReliesOnGetFieldInitialization = true;
                    successState.Initialization = InitializationKind.RecordFieldsInitialized;
                }

                RestoreStates(trueSeed);
                Visit(operation.IfTrueStatement);
                var getTrueStates = CloneStates();

                RestoreStates(falseSeed);
                if (operation.IfFalseStatement is not null)
                {
                    Visit(operation.IfFalseStatement);
                }
                var getFalseStates = CloneStates();

                _states = MergeStates(getTrueStates, getFalseStates);
                return;
            }

            Visit(operation.Condition);
            var beforeBranches = CloneStates();

            RestoreStates(beforeBranches);
            Visit(operation.IfTrueStatement);
            var trueStates = CloneStates();

            RestoreStates(beforeBranches);
            if (operation.IfFalseStatement is not null)
            {
                Visit(operation.IfFalseStatement);
            }
            var falseStates = CloneStates();

            _states = MergeStates(trueStates, falseStates);
        }

        public override void VisitCaseStatement(ICaseStatement operation)
        {
            Visit(operation.Value);
            var beforeBranches = CloneStates();
            var branches = new List<Dictionary<string, CandidateState>>();

            foreach (var caseLine in operation.CaseLines)
            {
                RestoreStates(beforeBranches);
                foreach (var expression in caseLine.Expressions)
                {
                    Visit(expression);
                }
                Visit(caseLine.Statement);
                branches.Add(CloneStates());
            }

            RestoreStates(beforeBranches);
            if (operation.ElseStatement is not null)
            {
                Visit(operation.ElseStatement);
            }
            branches.Add(CloneStates());

            _states = MergeStates(branches);
        }

        public override void VisitWhileRepeatLoopStatement(IWhileRepeatLoopStatement operation)
        {
            if (operation.LoopKind == EnumProvider.LoopKind.Repeat)
            {
                Visit(operation.Body);
                Visit(operation.Condition);
                var afterFirstRepeatIteration = CloneStates();

                RestoreStates(afterFirstRepeatIteration);
                Visit(operation.Body);
                Visit(operation.Condition);
                var afterSecondRepeatIteration = CloneStates();

                _states = MergeStates(afterFirstRepeatIteration, afterSecondRepeatIteration);
                return;
            }

            Visit(operation.Condition);
            var zeroIterations = CloneStates();

            RestoreStates(zeroIterations);
            Visit(operation.Body);
            Visit(operation.Condition);
            var afterFirstIteration = CloneStates();

            RestoreStates(afterFirstIteration);
            Visit(operation.Body);
            Visit(operation.Condition);
            var afterSecondIteration = CloneStates();

            _states = MergeStates([zeroIterations, afterFirstIteration, afterSecondIteration]);
        }

        public override void VisitForLoopStatement(IForLoopStatement operation)
        {
            Visit(operation.LoopVariable);
            Visit(operation.InitialValue);
            Visit(operation.EndValue);
            VisitOptionalLoopBodyTwice(operation.Body);
        }

        public override void VisitForEachLoopStatement(IForEachLoopStatement operation)
        {
            Visit(operation.IterationVariable);
            Visit(operation.Expression);
            VisitOptionalLoopBodyTwice(operation.Body);
        }

        public override void VisitExitStatement(IExitStatement operation)
        {
            if (operation.ReturnedValue is not null)
            {
                Visit(operation.ReturnedValue);
            }

            foreach (var state in _states.Values)
            {
                state.IsReachable = false;
            }
        }

        public override void VisitAssertErrorStatement(IAssertErrorStatement operation)
        {
            foreach (var state in _states.Values)
            {
                state.IsUnsafe = true;
            }
        }

        public override void VisitBreakStatement(IBreakStatement operation)
        {
            MarkAllCandidatesUnsafe();
        }

        private void VisitOptionalLoopBodyTwice(IOperation body)
        {
            var zeroIterations = CloneStates();

            RestoreStates(zeroIterations);
            Visit(body);
            var afterFirstIteration = CloneStates();

            RestoreStates(afterFirstIteration);
            Visit(body);
            var afterSecondIteration = CloneStates();

            _states = MergeStates([zeroIterations, afterFirstIteration, afterSecondIteration]);
        }

        private bool TryHandleClear(IInvocationExpression operation)
        {
            if (operation.TargetMethod?.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
                !SemanticFacts.IsSameName(operation.TargetMethod.Name, "Clear") ||
                operation.Arguments.Length != 1 ||
                !TryGetDirectCandidate(operation.Arguments[0].Value, out var candidate, out var state))
            {
                return false;
            }

            if (state.IsReachable)
            {
                state.Initialization = InitializationKind.FullyInitialized;
            }

            return true;
        }

        private bool TryHandleClearAll(IInvocationExpression operation)
        {
            if (operation.TargetMethod?.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
                !SemanticFacts.IsSameName(operation.TargetMethod.Name, "ClearAll"))
            {
                return false;
            }

            MarkAllCandidatesUnsafe();
            return true;
        }

        private bool TryHandleRecordGet(IInvocationExpression operation, bool resultIsDiscarded)
        {
            if (!resultIsDiscarded ||
                operation.TargetMethod?.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
                !SemanticFacts.IsSameName(operation.TargetMethod.Name, "Get") ||
                operation.Arguments.Length == 0 ||
                !TryGetDirectCandidate(operation.Instance, out var candidate, out var state) ||
                candidate.Type.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Record)
            {
                return false;
            }

            foreach (var argument in operation.Arguments)
            {
                Visit(argument.Value);
            }

            if (state.IsReachable && !state.IsUnsafe &&
                state.Initialization < InitializationKind.RecordFieldsInitialized)
            {
                state.ReliesOnGetFieldInitialization = true;
                state.Initialization = InitializationKind.RecordFieldsInitialized;
            }

            return true;
        }

        private bool TryVisitConditionalRecordGet(
            IOperation condition,
            out IVariableSymbol candidate,
            out bool successOnTrueBranch)
        {
            candidate = null!;
            successOnTrueBranch = true;
            condition = condition.UnwrapConversions();

            if (condition is IUnaryOperatorExpression unary &&
                (unary.UnaryOperationKind == EnumProvider.UnaryOperationKind.BooleanLogicalNot ||
                 unary.UnaryOperationKind == EnumProvider.UnaryOperationKind.OperatorMethodLogicalNot))
            {
                successOnTrueBranch = false;
                condition = unary.Operand.UnwrapConversions();
            }

            if (condition is not IInvocationExpression invocation ||
                invocation.TargetMethod?.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
                !SemanticFacts.IsSameName(invocation.TargetMethod.Name, "Get") ||
                invocation.Arguments.Length == 0 ||
                !TryGetDirectCandidate(invocation.Instance, out candidate, out var state) ||
                candidate.Type.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Record)
            {
                candidate = null!;
                return false;
            }

            foreach (var argument in invocation.Arguments)
            {
                Visit(argument.Value);
            }

            return true;
        }

        private static bool IsFlowTerminatingCall(IInvocationExpression operation)
        {
            if (operation.TargetMethod?.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
                !SemanticFacts.IsSameName(operation.TargetMethod.Name, "Error") ||
                operation.Arguments.Length == 0 ||
                operation.Arguments[0].Value.Type is not { } argumentType)
            {
                return false;
            }

            return argumentType.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.ErrorInfo;
        }

        private void InvalidateInitializedStateForPotentialReentrancy()
        {
            foreach (var state in _states.Values)
            {
                if (state.IsReachable)
                {
                    state.Initialization = InitializationKind.Unknown;
                }
            }
        }

        private void MarkAllCandidatesUnsafe()
        {
            foreach (var state in _states.Values)
            {
                state.IsUnsafe = true;
            }
        }

        private static void Require(CandidateState state, InitializationKind required)
        {
            if (state.IsReachable && state.Initialization < required)
            {
                state.IsUnsafe = true;
            }
        }

        private bool TryGetDirectCandidate(
            IOperation? operation,
            out IVariableSymbol candidate,
            out CandidateState state)
        {
            candidate = null!;
            state = null!;

            if (operation is null)
            {
                return false;
            }

            operation = operation.UnwrapConversions();
            return operation is IGlobalReferenceExpression globalReference &&
                TryGetCandidate(globalReference.GlobalVariable, out candidate, out state);
        }

        private bool TryGetCandidate(
            ISymbol? symbol,
            out IVariableSymbol candidate,
            out CandidateState state)
        {
            candidate = null!;
            state = null!;

            if (symbol is not IVariableSymbol variable ||
                !_candidates.TryGetValue(variable.Name, out var possibleCandidate) ||
                !IsSameVariable(variable, possibleCandidate))
            {
                return false;
            }

            candidate = possibleCandidate;
            state = _states[possibleCandidate.Name];
            return true;
        }

        private bool ContainsCandidate(IOperation operation)
        {
            var finder = new CandidateReferenceFinder(_candidates);
            finder.Visit(operation);
            return finder.FoundNames.Count > 0;
        }

        private void MarkReferencedCandidatesUnsafe(IOperation operation)
        {
            var finder = new CandidateReferenceFinder(_candidates);
            finder.Visit(operation);

            foreach (var name in finder.FoundNames)
            {
                _states[name].IsUnsafe = true;
            }
        }

        private Dictionary<string, CandidateState> CloneStates()
        {
            var clone = new Dictionary<string, CandidateState>(SemanticFacts.NameEqualityComparer);
            foreach (var pair in _states)
            {
                clone.Add(pair.Key, pair.Value.Clone());
            }
            return clone;
        }

        private static Dictionary<string, CandidateState> CloneStateDictionary(
            Dictionary<string, CandidateState> states)
        {
            var clone = new Dictionary<string, CandidateState>(SemanticFacts.NameEqualityComparer);
            foreach (var pair in states)
            {
                clone.Add(pair.Key, pair.Value.Clone());
            }
            return clone;
        }

        private void RestoreStates(Dictionary<string, CandidateState> states)
        {
            _states = new Dictionary<string, CandidateState>(SemanticFacts.NameEqualityComparer);
            foreach (var pair in states)
            {
                _states.Add(pair.Key, pair.Value.Clone());
            }
        }

        private static Dictionary<string, CandidateState> MergeStates(
            Dictionary<string, CandidateState> first,
            Dictionary<string, CandidateState> second) =>
            MergeStates([first, second]);

        private static Dictionary<string, CandidateState> MergeStates(
            IEnumerable<Dictionary<string, CandidateState>> branches)
        {
            Dictionary<string, CandidateState>? merged = null;

            foreach (var branch in branches)
            {
                if (merged is null)
                {
                    merged = new Dictionary<string, CandidateState>(SemanticFacts.NameEqualityComparer);
                    foreach (var pair in branch)
                    {
                        merged.Add(pair.Key, pair.Value.Clone());
                    }
                    continue;
                }

                foreach (var pair in merged)
                {
                    var other = branch[pair.Key];
                    pair.Value.IsUnsafe |= other.IsUnsafe;
                    pair.Value.ReliesOnGetFieldInitialization |= other.ReliesOnGetFieldInitialization;
                    pair.Value.HasPersistentRecordStateMutation |= other.HasPersistentRecordStateMutation;

                    if (!pair.Value.IsReachable)
                    {
                        pair.Value.Initialization = other.Initialization;
                        pair.Value.IsReachable = other.IsReachable;
                    }
                    else if (other.IsReachable)
                    {
                        pair.Value.Initialization =
                            (InitializationKind)Math.Min((int)pair.Value.Initialization, (int)other.Initialization);
                    }
                }
            }

            return merged ?? new Dictionary<string, CandidateState>(SemanticFacts.NameEqualityComparer);
        }

        private sealed class CandidateReferenceFinder(
            IReadOnlyDictionary<string, IVariableSymbol> candidates) : OperationWalker
        {
            private readonly IReadOnlyDictionary<string, IVariableSymbol> _candidates = candidates;

            public HashSet<string> FoundNames { get; } = new(SemanticFacts.NameEqualityComparer);

            public override void Visit(IOperation operation)
            {
                if (operation is IGlobalReferenceExpression globalReference &&
                    globalReference.GlobalVariable is IVariableSymbol variable &&
                    _candidates.TryGetValue(variable.Name, out var candidate) &&
                    IsSameVariable(variable, candidate))
                {
                    FoundNames.Add(candidate.Name);
                    return;
                }

                base.Visit(operation);
            }
        }
    }
}
