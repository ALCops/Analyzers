using System.Collections.Immutable;
using ALCops.Common;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class TryFunctionDatabaseModification : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> DirectDatabaseModificationMethods =
        ImmutableHashSet.Create(
            SemanticFacts.NameEqualityComparer,
            "Truncate",
            "AddLink",
            "CopyLinks",
            "DeleteLink",
            "DeleteLinks");

    private static readonly ImmutableHashSet<string> PotentialDatabaseModificationMethods =
        ImmutableHashSet.Create(
            SemanticFacts.NameEqualityComparer,
            "Commit",
            "Execute",
            "Print",
            "Run",
            "RunModal",
            "RunRequestPage",
            "SaveAs",
            "SaveAsExcel",
            "SaveAsHtml",
            "SaveAsPdf",
            "SaveAsWord",
            "SaveAsXml",
            "Validate");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.TryFunctionDatabaseModificationDisallowed,
            DiagnosticDescriptors.TryFunctionPotentialDatabaseModification);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCodeBlockAction(AnalyzeCodeBlock);

    private static void AnalyzeCodeBlock(CodeBlockAnalysisContext ctx)
    {
        if (ctx.IsObsolete()
            || ctx.CodeBlock is not MethodOrTriggerDeclarationSyntax methodSyntax
            || methodSyntax is not MethodDeclarationSyntax methodDeclaration
            || methodDeclaration.Body is null
            || !methodDeclaration.IsTryFunction())
        {
            return;
        }

        foreach (InvocationExpressionSyntax invocation in methodDeclaration.Body
                     .DescendantNodes()
                     .OfType<InvocationExpressionSyntax>())
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            if (ctx.SemanticModel.GetSymbolInfo(invocation, ctx.CancellationToken).Symbol is not IMethodSymbol method)
                continue;

            if (method.MethodKind == EnumProvider.MethodKind.BuiltInMethod
                && IsDirectDatabaseModificationMethod(method.Name))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TryFunctionDatabaseModificationDisallowed,
                    invocation.GetLocation(),
                    method.Name));
                continue;
            }

            if (method.MethodKind != EnumProvider.MethodKind.BuiltInMethod
                || PotentialDatabaseModificationMethods.Contains(method.Name))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TryFunctionPotentialDatabaseModification,
                    invocation.GetLocation(),
                    method.Name));
            }
        }
    }

    private static bool IsDirectDatabaseModificationMethod(string methodName) =>
        RecordMethodClassification.InsertMethods.Contains(methodName)
        || RecordMethodClassification.ModifyMethods.Contains(methodName)
        || RecordMethodClassification.DeleteMethods.Contains(methodName)
        || DirectDatabaseModificationMethods.Contains(methodName);
}
