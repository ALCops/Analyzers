using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PageVariableSetRecordTemporaryRecord : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.PageVariableSetRecordTemporaryRecord);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        var targetMethod = invocation.TargetMethod;
        if (targetMethod is null || targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        if (!SemanticFacts.IsSameName(targetMethod.Name, "SetRecord"))
            return;

        if (targetMethod.ContainingType?.GetTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Page)
            return;

        if (invocation.Arguments.Length != 1)
            return;

        var recordArgument = invocation.Arguments[0];
        if (recordArgument.Value is not IConversionExpression conversion)
            return;

        var operand = conversion.Operand;
        if (operand?.Type is not IRecordTypeSymbol recordType)
            return;

        if (!recordType.IsTemporary())
            return;

        // CurrPage, page variables and CurrPage in a pageextension all resolve to the page
        // through the instance type's original definition.
        if (invocation.Instance?.Type?.OriginalDefinition is not IPageTypeSymbol page)
            return;

        // SetRecord only positions the page on the record's key. A page whose source is
        // temporary, or that serves its own dataset through OnFindRecord, can show the
        // record when it loads it itself; only a database-backed page never shows it.
        if (page.GetBooleanPropertyValue(EnumProvider.PropertyKind.SourceTableTemporary).GetValueOrDefault())
            return;

        if (page.RelatedTable is null || page.RelatedTable.IsTemporary())
            return;

        if (page.GetTriggerSymbol("OnFindRecord") is not null)
            return;

        var receiverName = invocation.Instance.Type.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.CurrPage
            ? "CurrPage"
            : invocation.Instance.GetSymbolSafe()?.Name ?? string.Empty;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PageVariableSetRecordTemporaryRecord,
            ctx.Operation.Syntax.GetLocation(),
            receiverName,
            page.Name));
    }
}
