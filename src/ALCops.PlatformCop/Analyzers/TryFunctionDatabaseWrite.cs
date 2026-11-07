using System.Collections.Immutable;
using ALCops.Common;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class TryFunctionDatabaseWrite : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> DatabaseWriteMethods =
        RecordMethodClassification.InsertMethods
            .Union(RecordMethodClassification.ModifyMethods)
            .Union(RecordMethodClassification.DeleteMethods)
            .Union(ImmutableHashSet.Create(
                SemanticFacts.NameEqualityComparer,
                "Truncate",
                "AddLink",
                "CopyLinks",
                "DeleteLink",
                "DeleteLinks"));

    private static readonly ImmutableHashSet<string> PotentialDatabaseWriteMethods =
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
            DiagnosticDescriptors.TryFunctionDatabaseWrite,
            DiagnosticDescriptors.TryFunctionPotentialDatabaseWrite);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCodeBlockAction(AnalyzeCodeBlock);

    private static void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
    {
        if (context.IsObsolete()
            || context.CodeBlock is not MethodDeclarationSyntax { Body: { } body } method
            || !method.IsTryFunction())
        {
            return;
        }

        var operation = context.SemanticModel.GetOperation(body, context.CancellationToken);
        if (operation is null)
            return;

        new DatabaseWriteWalker(context).Visit(operation);
    }

    private sealed class DatabaseWriteWalker : OperationWalker
    {
        private readonly CodeBlockAnalysisContext _context;

        public DatabaseWriteWalker(CodeBlockAnalysisContext context) => _context = context;

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            _context.CancellationToken.ThrowIfCancellationRequested();

            var method = operation.TargetMethod;
            if (method.MethodKind == EnumProvider.MethodKind.BuiltInMethod
                && DatabaseWriteMethods.Contains(method.Name)
                && operation.GetReceiverTableType(_context.OwningSymbol, out var recordType) is { } table
                && !table.IsTemporary()
                && recordType?.IsTemporary() != true)
            {
                _context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TryFunctionDatabaseWrite,
                    operation.Syntax.GetLocation(),
                    method.Name));
            }
            else if (method.MethodKind != EnumProvider.MethodKind.BuiltInMethod
                || PotentialDatabaseWriteMethods.Contains(method.Name))
            {
                _context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TryFunctionPotentialDatabaseWrite,
                    operation.Syntax.GetLocation(),
                    method.Name));
            }

            base.VisitInvocationExpression(operation);
        }
    }
}
