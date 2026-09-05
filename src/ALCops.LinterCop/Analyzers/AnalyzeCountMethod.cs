using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class AnalyzeCountMethod : DiagnosticAnalyzer
{
    private const string CountMethodName = "Count";
    private const int Zero = 0;
    private const int One = 1;
    private const int Two = 2;
    private const int MaxRelevantValue = 2;

    // Tables with one of these identifiers in the name could possible have a large amount of records
    private static readonly HashSet<string> possibleLargeTableIdentifierKeywords = new HashSet<string>
    {
        "Ledger", "GL", "G/L",
        "Posted", "Pstd",
        "Log",
        "Entry",
        "Archive",
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.UseIsEmptyMethodInsteadOfCount,
            DiagnosticDescriptors.UseQueryOrFindWithNextInsteadOfCount);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeCountInvocation,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeCountInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        if (invocation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
            !string.Equals(invocation.TargetMethod.Name, CountMethodName, StringComparison.Ordinal) ||
            invocation.TargetMethod.ContainingSymbol?.Name != "Table")
            return;

        var tableType = invocation.Instance.GetReceiverTableType(ctx.ContainingSymbol, out var recordType);
        if (tableType is null)
            return;

        if (recordType is not null && recordType.Temporary)
            return;

        if (invocation.Syntax.Parent is not BinaryExpressionSyntax binaryExpression)
            return;

        int rightValue = GetLiteralExpressionValue(binaryExpression.Right);
        if (rightValue > MaxRelevantValue)
            return;

        int leftValue = GetLiteralExpressionValue(binaryExpression.Left);
        if (leftValue > MaxRelevantValue)
            return;

        var symbolName = GetSymbolName(invocation, tableType);

        if (IsZeroComparison(leftValue, rightValue))
        {
            ReportUseIsEmptyDiagnostic(ctx, invocation, symbolName);
            return;
        }

        if (IsLessThanOneComparison(binaryExpression, rightValue) || IsGreaterThanOneComparison(binaryExpression, leftValue))
        {
            ReportUseIsEmptyDiagnostic(ctx, invocation, symbolName);
            return;
        }

        if (IsEligibleUseQueryOrFindWithNext(tableType))
        {
            if (IsOneComparison(binaryExpression, leftValue, rightValue))
            {
                ReportUseFindWithNextDiagnostic(ctx, invocation, binaryExpression, leftValue, rightValue, symbolName);
                return;
            }

            if (IsLessThanTwoComparison(binaryExpression, rightValue) || IsGreaterThanTwoComparison(binaryExpression, leftValue))
            {
                ReportUseFindWithNextDiagnostic(ctx, invocation, binaryExpression, leftValue, rightValue, symbolName);
                return;
            }
        }
    }

    private static int GetLiteralExpressionValue(CodeExpressionSyntax codeExpression)
    {
        if (codeExpression is not LiteralExpressionSyntax literal)
            return -1;

        if (literal.Literal.Kind != EnumProvider.SyntaxKind.Int32SignedLiteralValue)
            return -1;

        return literal.Literal.GetLiteralValue() is int value ? value : -1;
    }

    private static SyntaxKind FlipOperator(SyntaxKind kind) => kind switch
    {
        _ when kind == EnumProvider.SyntaxKind.LessThanToken => EnumProvider.SyntaxKind.GreaterThanToken,
        _ when kind == EnumProvider.SyntaxKind.GreaterThanToken => EnumProvider.SyntaxKind.LessThanToken,
        _ when kind == EnumProvider.SyntaxKind.LessThanEqualsToken => EnumProvider.SyntaxKind.GreaterThanEqualsToken,
        _ when kind == EnumProvider.SyntaxKind.GreaterThanEqualsToken => EnumProvider.SyntaxKind.LessThanEqualsToken,
        _ => kind, // '=' and '<>' are symmetric
    };

    private static string GetOperatorSign(SyntaxKind kind) => kind switch
    {
        _ when kind == EnumProvider.SyntaxKind.EqualsToken => "=",
        _ when kind == EnumProvider.SyntaxKind.NotEqualsToken => "<>",
        _ when kind == EnumProvider.SyntaxKind.LessThanToken => "<",
        _ when kind == EnumProvider.SyntaxKind.GreaterThanToken => ">",
        _ when kind == EnumProvider.SyntaxKind.LessThanEqualsToken => "<=",
        _ when kind == EnumProvider.SyntaxKind.GreaterThanEqualsToken => ">=",
        _ => "=",
    };

    private static bool IsZeroComparison(int left, int right)
        => left == Zero || right == Zero;

    private static bool IsLessThanOneComparison(BinaryExpressionSyntax expr, int right) =>
             expr.OperatorToken.Kind == EnumProvider.SyntaxKind.LessThanToken && right == One;

    private static bool IsGreaterThanOneComparison(BinaryExpressionSyntax expr, int left) =>
        expr.OperatorToken.Kind == EnumProvider.SyntaxKind.GreaterThanToken && left == One;

    private static bool IsComparisonOperator(SyntaxKind kind) =>
        kind is var k &&
        (k == EnumProvider.SyntaxKind.EqualsToken ||
         k == EnumProvider.SyntaxKind.NotEqualsToken ||
         k == EnumProvider.SyntaxKind.LessThanToken ||
         k == EnumProvider.SyntaxKind.GreaterThanToken ||
         k == EnumProvider.SyntaxKind.LessThanEqualsToken ||
         k == EnumProvider.SyntaxKind.GreaterThanEqualsToken);

    private static bool IsOneComparison(BinaryExpressionSyntax expr, int left, int right) =>
        IsComparisonOperator(expr.OperatorToken.Kind) && (left == One || right == One);

    private static bool IsLessThanTwoComparison(BinaryExpressionSyntax expr, int right) =>
        expr.OperatorToken.Kind == EnumProvider.SyntaxKind.LessThanToken && right == Two;

    private static bool IsGreaterThanTwoComparison(BinaryExpressionSyntax expr, int left) =>
        expr.OperatorToken.Kind == EnumProvider.SyntaxKind.GreaterThanToken && left == Two;

    private static bool IsEligibleUseQueryOrFindWithNext(ITableTypeSymbol table)
    {
        if (possibleLargeTableIdentifierKeywords.Any(keyword => table.Name.Contains(keyword, SemanticFacts.NameEqualityComparison)))
            return true;

        return table.PrimaryKey.Fields.Any(field => SemanticFacts.IsSameName(field.Name, "Entry No."));
    }

    private static void ReportUseIsEmptyDiagnostic(OperationAnalysisContext ctx, IInvocationExpression operation, string symbolName)
    {
        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UseIsEmptyMethodInsteadOfCount,
            operation.Syntax.Parent.GetLocation(),
            symbolName));
    }

    private static void ReportUseFindWithNextDiagnostic(OperationAnalysisContext ctx, IInvocationExpression operation, BinaryExpressionSyntax binaryExpression, int leftValue, int rightValue, string symbolName)
    {
        // Normalize to "Count() <op> <value>" so the message always reads left-to-right,
        // even when the source has the literal on the left (e.g. "2 > Rec.Count()").
        bool countIsOnRight = leftValue >= 0 && rightValue < 0;
        int comparedValue = countIsOnRight ? leftValue : rightValue;
        SyntaxKind operatorKind = countIsOnRight
            ? FlipOperator(binaryExpression.OperatorToken.Kind)
            : binaryExpression.OperatorToken.Kind;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UseQueryOrFindWithNextInsteadOfCount,
            operation.Syntax.Parent.GetLocation(),
            symbolName,
            GetOperatorSign(operatorKind),
            comparedValue));
    }

    private static string GetSymbolName(IInvocationExpression operation, ITableTypeSymbol tableType) =>
        operation.Instance?.GetSymbolSafe()?.Name.QuoteIdentifierIfNeededWithReflection()
            ?? tableType.Name.QuoteIdentifierIfNeededWithReflection();
}