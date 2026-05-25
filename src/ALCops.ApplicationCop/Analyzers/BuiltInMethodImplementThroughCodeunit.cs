using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class BuiltInMethodImplementThroughCodeunit : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.ConfirmImplementConfirmManagement,
            DiagnosticDescriptors.GlobalLanguageImplementTranslationHelper
        );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            this.AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression operation)
            return;

        if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        switch (operation.TargetMethod.Name)
        {
            case "Confirm":
                var containingType = ctx.ContainingSymbol.GetContainingObjectTypeSymbol();

                // If it's a non-API Page, skip.
                if (containingType.NavTypeKind == EnumProvider.NavTypeKind.Page &&
                    containingType is IPageTypeSymbol page &&
                    page.PageType != PageTypeKind.API)
                {
                    return;
                }

                if (containingType.NavTypeKind == EnumProvider.NavTypeKind.PageExtension)
                {
                    return;
                }

                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ConfirmImplementConfirmManagement,
                    ctx.Operation.Syntax.GetLocation()));
                break;

            case "GlobalLanguage":
                if (operation.Arguments.Length == 0)
                    return;

                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.GlobalLanguageImplementTranslationHelper,
                    ctx.Operation.Syntax.GetLocation()));
                break;
        }
    }
}