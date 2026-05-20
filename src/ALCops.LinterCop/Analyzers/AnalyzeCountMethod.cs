using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public class AnalyzeCountMethod : DiagnosticAnalyzer
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

        if (invocation.Instance?.Type is not IRecordTypeSymbol recordTypeSymbol || recordTypeSymbol.Temporary)
            return;

        if (invocation.Syntax.Parent is not BinaryExpressionSyntax binaryExpression)
            return;

        int rightValue = GetLiteralExpressionValue(binaryExpression.Right);
        if (rightValue > MaxRelevantValue)
            return;

        int leftValue = GetLiteralExpressionValue(binaryExpression.Left);
        if (leftValue > MaxRelevantValue)
            return;

        if (IsZeroComparison(leftValue, rightValue))
        {
            ReportUseIsEmptyDiagnostic(ctx, invocation);
            return;
        }

        if (IsLessThanOneComparison(binaryExpression, rightValue) || IsGreaterThanOneComparison(binaryExpression, leftValue))
        {
            ReportUseIsEmptyDiagnostic(ctx, invocation);
            return;
        }

        if (IsEligibleUseQueryOrFindWithNext(recordTypeSymbol))
        {
            if (IsOneComparison(leftValue, rightValue))
            {
                ReportUseFindWithNextDiagnostic(ctx, invocation, GetOperatorKind(binaryExpression.OperatorToken.Kind));
                return;
            }

            if (IsLessThanTwoComparison(binaryExpression, rightValue) || IsGreaterThanTwoComparison(binaryExpression, leftValue))
            {
                ReportUseFindWithNextDiagnostic(ctx, invocation, EnumProvider.SyntaxKind.EqualsToken);
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

    private static SyntaxKind GetOperatorKind(SyntaxKind tokenKind) =>
        tokenKind == EnumProvider.SyntaxKind.EqualsToken ? EnumProvider.SyntaxKind.EqualsToken : EnumProvider.SyntaxKind.NotEqualsToken;

    private static bool IsZeroComparison(int left, int right)
        => left == Zero || right == Zero;

    private static bool IsLessThanOneComparison(BinaryExpressionSyntax expr, int right) =>
             expr.OperatorToken.Kind == EnumProvider.SyntaxKind.LessThanToken && right == One;

    private static bool IsGreaterThanOneComparison(BinaryExpressionSyntax expr, int left) =>
        expr.OperatorToken.Kind == EnumProvider.SyntaxKind.GreaterThanToken && left == One;

    private static bool IsOneComparison(int left, int right) =>
        left == One || right == One;

    private static bool IsLessThanTwoComparison(BinaryExpressionSyntax expr, int right) =>
        expr.OperatorToken.Kind == EnumProvider.SyntaxKind.LessThanToken && right == Two;

    private static bool IsGreaterThanTwoComparison(BinaryExpressionSyntax expr, int left) =>
        expr.OperatorToken.Kind == EnumProvider.SyntaxKind.GreaterThanToken && left == Two;

    private static bool IsEligibleUseQueryOrFindWithNext(IRecordTypeSymbol record)
    {
        if (possibleLargeTableIdentifierKeywords.Any(keyword => record.Name.IndexOf(keyword, SemanticFacts.NameEqualityComparison) >= 0))
            return true;

        // Tables with a field "Entry No." could possible have a large amount of records
        if (record.OriginalDefinition is ITableTypeSymbol table)
            return table.PrimaryKey.Fields.Any(field => SemanticFacts.IsSameName(field.Name, "Entry No."));

        return false;
    }

    private static void ReportUseIsEmptyDiagnostic(OperationAnalysisContext ctx, IInvocationExpression operation)
    {
        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UseIsEmptyMethodInsteadOfCount,
            operation.Syntax.Parent.GetLocation(),
            GetSymbolName(operation)));
    }

    private static void ReportUseFindWithNextDiagnostic(OperationAnalysisContext ctx, IInvocationExpression operation, SyntaxKind operatorToken)
    {
        string operatorSign = operatorToken == EnumProvider.SyntaxKind.EqualsToken ? "=" : "<>";

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UseQueryOrFindWithNextInsteadOfCount,
            operation.Syntax.Parent.GetLocation(),
            GetSymbolName(operation), operatorSign));
    }

    private static string GetSymbolName(IInvocationExpression operation) =>
            operation.Instance?.GetSymbol()?.Name.QuoteIdentifierIfNeededWithReflection() ?? string.Empty;
}