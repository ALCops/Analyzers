using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.TestAutomationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class InvokeActionOnPartTestPage : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.InvokeActionOnPartTestPage);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        var method = invocation.TargetMethod;
        if (method.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
            method.ContainingSymbol is not IClassTypeSymbol cls ||
            !SemanticFacts.IsSameName(cls.Name, "TestAction"))
            return;

        // MainPage.Part.Action binds its receiver as a TestPart; only a TestPage receiver is a part opened directly.
        if (invocation.Instance is not ITestActionAccess access)
            return;

        var receiverType = access.Instance?.Type;
        if (receiverType is null || receiverType.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.TestPage)
            return;

        if (receiverType.OriginalDefinition is not IPageTypeSymbol page)
            return;

        var pageType = page.PageType;
        if (pageType != EnumProvider.PageTypeKind.ListPart && pageType != EnumProvider.PageTypeKind.CardPart)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.InvokeActionOnPartTestPage,
            invocation.Syntax.GetLocation(),
            access.ActionSymbol.Name, page.Name, pageType.ToString()));
    }
}
