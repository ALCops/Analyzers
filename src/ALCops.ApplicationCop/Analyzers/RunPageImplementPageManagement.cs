using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class RunPageImplementPageManagement : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.RunPageImplementPageManagement);

    private static readonly ImmutableDictionary<int, string> SupportedRecords =
        new Dictionary<int, string>
        {
            { 36, "Sales Header" },
            { 38, "Purchase Header" },
            { 79, "Company Information" },
            { 80, "Gen. Journal Template" },
            { 81, "Gen. Journal Line" },
            { 91, "User Setup" },
            { 98, "General Ledger Setup" },
            { 112, "Sales Invoice Header" },
            { 131, "Incoming Documents Setup" },
            { 207, "Res. Journal Line" },
            { 210, "Job Journal Line" },
            { 232, "Gen. Journal Batch" },
            { 312, "Purchases & Payables Setup" },
            { 454, "Approval Entry" },
            { 843, "Cash Flow Setup" },
            { 1251, "Text-to-Account Mapping" },
            { 1275, "Doc. Exch. Service Setup" },
            { 5107, "Sales Header Archive" },
            { 5109, "Purchase Header Archive" },
            { 5200, "Employee" },
            { 5405, "Production Order" },
            { 5900, "Service Header" },
            { 5965, "Service Contract Header" },
            { 7152, "Item Analysis View" },
            { 2000000120, "User" }
        }.ToImmutableDictionary();

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            this.CheckRunPageImplementPageManagement,
                EnumProvider.OperationKind.InvocationExpression);

    private void CheckRunPageImplementPageManagement(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        var targetMethod = invocation.TargetMethod;

        if (targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod || invocation.Arguments.Length < 2)
            return;

        // do not execute on CurrPage.EnqueueBackgroundTask
        if (string.Equals(targetMethod.Name, "EnqueueBackgroundTask", StringComparison.Ordinal))
            return;

        var containingType = targetMethod.ContainingType?.GetTypeSymbol();
        if (containingType is null || containingType.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Page)
            return;

        // Page Management Codeunit doesn't support returntype Action
        var returnType = targetMethod.ReturnValueSymbol.ReturnType;
        if (returnType.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Action &&
            IsReturnValueUsedSyntax(invocation))
            return;

        var syntaxKind = invocation.Arguments[0].Syntax.Kind;
        switch (syntaxKind)
        {
            case var _ when syntaxKind == EnumProvider.SyntaxKind.LiteralExpression:
                if (invocation.Arguments[0].Syntax.GetIdentifierOrLiteralValue() == "0")
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.RunPageImplementPageManagement,
                        ctx.Operation.Syntax.GetLocation()));
                break;

            case var _ when syntaxKind == EnumProvider.SyntaxKind.OptionAccessExpression:
                if (IsSupportedRecord(((IConversionExpression)invocation.Arguments[1].Value).Operand))
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.RunPageImplementPageManagement,
                        ctx.Operation.Syntax.GetLocation()));
                break;
        }
    }

    private static bool IsReturnValueUsedSyntax(IInvocationExpression invocation)
    {
        var syntax = invocation.Syntax;

        if (syntax.Parent is ExpressionStatementSyntax exprStmt &&
            ReferenceEquals(exprStmt.Expression, syntax))
            return false;

        return true;
    }

    private static bool IsSupportedRecord(IOperation operation)
    {
        var kind = operation.Kind;

        IRecordTypeSymbol? recordTypeSymbol;
        switch (kind)
        {
            case var _ when kind == EnumProvider.OperationKind.GlobalReferenceExpression:
            case var _ when kind == EnumProvider.OperationKind.LocalReferenceExpression:
                recordTypeSymbol = operation.GetSymbol()?.GetTypeSymbol() as IRecordTypeSymbol;
                break;
            case var _ when kind == EnumProvider.OperationKind.InvocationExpression:
                recordTypeSymbol = operation.Type.GetTypeSymbol() as IRecordTypeSymbol;
                break;
            default:
                return false;
        }

        if (recordTypeSymbol is null || recordTypeSymbol.Temporary)
            return false;

        return SupportedRecords.TryGetValue(recordTypeSymbol.Id, out var recordName) &&
               SemanticFacts.IsSameName(recordTypeSymbol.Name, recordName);
    }
}