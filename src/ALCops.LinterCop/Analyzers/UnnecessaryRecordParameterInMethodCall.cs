using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class UnnecessaryRecordParameterInMethodCall : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.UnnecessaryRecordParameterInMethodCall);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        if (invocation.Arguments.IsEmpty)
            return;

        if (invocation.TargetMethod.IsEvent)
            return;

        if (invocation.Instance is not null)
        {
            AnalyzeExternalCall(ctx, invocation);
            return;
        }

        AnalyzeInternalCall(ctx, invocation);
    }

    /// <summary>
    /// Handles the case where a method is called on a record variable using dot-notation:
    /// <c>MyRecord.MyProcedure(MyRecord)</c>
    /// </summary>
    private static void AnalyzeExternalCall(OperationAnalysisContext ctx, IInvocationExpression invocation)
    {
        if (invocation.Instance!.Type?.NavTypeKind != EnumProvider.NavTypeKind.Record)
            return;

        if (!IsInCurrentModule(ctx, invocation.TargetMethod))
            return;

        if (invocation.TargetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod)
            return;

        var instanceSymbol = invocation.Instance.GetSymbolSafe();
        if (instanceSymbol is null)
            return;

        foreach (var argument in invocation.Arguments)
        {
            var argumentSymbol = ResolveArgumentSymbol(argument);

            if (argumentSymbol is not null && instanceSymbol.Equals(argumentSymbol))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnnecessaryRecordParameterInMethodCall,
                    argument.Syntax.GetLocation(),
                    instanceSymbol.Name));
            }
        }
    }

    /// <summary>
    /// Handles the case where a method is called inside a table, page, or their extensions
    /// and the implicit <c>Rec</c> is passed as an argument:
    /// <c>MyProcedure(Rec)</c>
    /// For pages and page extensions, only local methods are flagged. Public/internal methods
    /// that accept the source record are considered intentional API design for decoupling.
    /// For tables and table extensions, all methods are flagged because the table IS the record.
    /// </summary>
    private static void AnalyzeInternalCall(OperationAnalysisContext ctx, IInvocationExpression invocation)
    {
        var containingObject = invocation.Syntax.GetContainingObjectSyntax();

        if (!IsRecordOwningObject(containingObject))
            return;

        if (IsPageObject(containingObject) && !invocation.TargetMethod.IsLocal)
            return;

        if (!IsInCurrentModule(ctx, invocation.TargetMethod))
            return;

        if (invocation.TargetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod)
            return;

        foreach (var argument in invocation.Arguments)
        {
            var argumentSymbol = ResolveArgumentSymbol(argument);

            if (argumentSymbol is not null
                && argumentSymbol.Kind == EnumProvider.SymbolKind.GlobalVariable
                && argumentSymbol.IsSynthesized
                && SemanticFacts.IsSameName(argumentSymbol.Name, "Rec"))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnnecessaryRecordParameterInMethodCall,
                    argument.Syntax.GetLocation(),
                    argumentSymbol.Name));
            }
        }
    }

    /// <summary>
    /// Resolves the symbol from an argument value, unwrapping through
    /// <c>IConversionExpression</c> when the SDK wraps the operand.
    /// Uses <see cref="OperationSafeExtensions.GetSymbolSafe"/> to guard against
    /// SDK bugs with <c>BoundApplicationObjectAccess</c>.
    /// </summary>
    private static ISymbol? ResolveArgumentSymbol(IArgument argument)
    {
        var symbol = argument.Value.GetSymbolSafe();
        if (symbol is not null)
            return symbol;

        if (argument.Value is IConversionExpression conversion)
            return conversion.Operand.GetSymbolSafe();

        return null;
    }

    /// <summary>
    /// Checks whether the syntax node is a table, page, or one of their extensions
    /// (objects that have an implicit <c>Rec</c> variable).
    /// </summary>
    private static bool IsRecordOwningObject(SyntaxNode? containingObject) =>
        containingObject is TableSyntax
            or PageSyntax
            or TableExtensionSyntax
            or PageExtensionSyntax;

    /// <summary>
    /// Checks whether the syntax node is a page or page extension.
    /// Used to apply the local-only restriction for page objects.
    /// </summary>
    private static bool IsPageObject(SyntaxNode? containingObject) =>
        containingObject is PageSyntax or PageExtensionSyntax;

    /// <summary>
    /// Ensures the target method is defined in the current module (the developer's own app).
    /// Methods from dependencies or the platform cannot be refactored, so flagging
    /// calls to them is not actionable.
    /// </summary>
    private static bool IsInCurrentModule(OperationAnalysisContext ctx, IMethodSymbol targetMethod)
    {
        var currentModule = ctx.ContainingSymbol.ContainingModule;
        var targetModule = targetMethod.ContainingModule;

        if (currentModule is null || targetModule is null)
            return false;

        return currentModule == targetModule;
    }
}
