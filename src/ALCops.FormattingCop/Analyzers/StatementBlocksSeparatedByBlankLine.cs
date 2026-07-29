using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.FormattingCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class StatementBlocksSeparatedByBlankLine : DiagnosticAnalyzer
{
    private const string ErrorMethodName = "Error";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.StatementBlocksSeparatedByBlankLine);

    // Single source of truth for control-flow kinds. Consumed both by the syntax-node registration
    // and by IsControlFlowStatement(); adding a new kind here is enough.
    private static readonly SyntaxKind[] ControlFlowStatementKindsArray =
    [
        EnumProvider.SyntaxKind.IfStatement,
        EnumProvider.SyntaxKind.CaseStatement,
        EnumProvider.SyntaxKind.RepeatStatement,
        EnumProvider.SyntaxKind.WhileStatement,
        EnumProvider.SyntaxKind.ForStatement,
        EnumProvider.SyntaxKind.ForEachStatement,
    ];

    private static readonly ImmutableHashSet<SyntaxKind> ControlFlowStatementKinds =
        ImmutableHashSet.Create(ControlFlowStatementKindsArray);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeControlFlowNode, ControlFlowStatementKindsArray);

        context.RegisterSyntaxNodeAction(
            AnalyzeExitStatement,
            EnumProvider.SyntaxKind.ExitStatement);

        context.RegisterOperationAction(
            AnalyzeInvocationExpression,
            EnumProvider.OperationKind.InvocationExpression);
    }

    private void AnalyzeControlFlowNode(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not StatementSyntax statement)
        {
            return;
        }

        var config = GetConfig(ctx.SemanticModel.Compilation.FileSystem);

        AnalyzeControlFlowStatement(ctx, statement, config);
        AnalyzeElseChain(ctx, statement, config);
    }

    private static void AnalyzeControlFlowStatement(
        SyntaxNodeAnalysisContext ctx,
        StatementSyntax statement,
        StatementBlockSpacingSettings config)
    {
        if (!config.ControlFlowBefore && !config.ControlFlowAfter)
        {
            return;
        }

        // Skip statements that live directly in an if's then/else slot. Those are branches, not
        // block siblings; the ElseChain check owns spacing before 'else', and there is nothing
        // meaningful to check "after" a branch.
        if (statement.Parent is IfStatementSyntax)
        {
            return;
        }

        if (IsOneLiner(statement) && !IncludesOneLiners(config))
        {
            return;
        }

        var statements = GetSiblingStatements(statement);

        if (statements.Length == 0)
        {
            return;
        }

        for (int i = 0; i < statements.Length; i++)
        {
            if (statements[i] != statement)
            {
                continue;
            }

            var statementName = GetControlFlowStatementName(statement);

            if (config.ControlFlowBefore && i > 0)
            {
                ReportMissingBlankLineBeforeIfNeeded(
                    ctx,
                    statements[i - 1],
                    statement.GetFirstToken(),
                    $"before '{statementName}' block");
            }

            if (config.ControlFlowAfter && i < statements.Length - 1)
            {
                var nextStatement = statements[i + 1];

                if (!IsControlFlowStatement(nextStatement))
                {
                    ReportMissingBlankLineBeforeIfNeeded(
                        ctx,
                        statement,
                        nextStatement.GetFirstToken(),
                        $"after '{statementName}' block");
                }
            }

            return;
        }
    }

    private static void AnalyzeElseChain(
        SyntaxNodeAnalysisContext ctx,
        StatementSyntax statement,
        StatementBlockSpacingSettings config)
    {
        if (config.ElseChainBeforeMode != ElseChainBeforeMode.RequireBlank)
        {
            return;
        }

        if (statement is not IfStatementSyntax ifStatement || ifStatement.ElseKeywordToken.IsMissing)
        {
            return;
        }

        var elseToken = ifStatement.ElseKeywordToken;
        var tokenBeforeElse = elseToken.GetPreviousToken();

        // GetPreviousToken() returns default(SyntaxToken) when no previous token exists; its Kind
        // is SyntaxKind.None. Compare via Kind property rather than struct equality with default.
        if (tokenBeforeElse.Kind == EnumProvider.SyntaxKind.None)
        {
            return;
        }

        if (elseToken.GetLocation().GetLineSpan().StartLinePosition.Line ==
            tokenBeforeElse.GetLocation().GetLineSpan().EndLinePosition.Line)
        {
            return;
        }

        if (!HasBlankLineBetween(tokenBeforeElse, elseToken))
        {
            ReportDiagnostic(ctx, elseToken, "before 'else' keyword");
        }
    }

    private void AnalyzeExitStatement(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not StatementSyntax statement)
        {
            return;
        }

        var config = GetConfig(ctx.SemanticModel.Compilation.FileSystem);

        if (!IncludesExit(config))
        {
            return;
        }

        var statements = GetSiblingStatements(statement);

        if (statements.Length == 0)
        {
            return;
        }

        for (int i = 0; i < statements.Length; i++)
        {
            if (statements[i] != statement)
            {
                continue;
            }

            if (i > 0)
            {
                ReportMissingBlankLineBeforeIfNeeded(
                    ctx,
                    statements[i - 1],
                    statement.GetFirstToken(),
                    "before scope-leaving statement 'exit'");
            }

            return;
        }
    }

    private void AnalyzeInvocationExpression(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
        {
            return;
        }

        if (invocation.Syntax.Parent is not ExpressionStatementSyntax expressionStatement)
        {
            return;
        }

        if (!IsBuiltInErrorInvocation(invocation))
        {
            return;
        }

        var config = GetConfig(ctx.Compilation.FileSystem);

        if (!IncludesError(config))
        {
            return;
        }

        var statements = GetSiblingStatements(expressionStatement);

        if (statements.Length == 0)
        {
            return;
        }

        for (int i = 0; i < statements.Length; i++)
        {
            if (statements[i] != expressionStatement)
            {
                continue;
            }

            if (i > 0)
            {
                ReportMissingBlankLineBeforeIfNeeded(
                    ctx,
                    statements[i - 1],
                    expressionStatement.GetFirstToken(),
                    "before scope-leaving statement 'Error()'");
            }

            return;
        }
    }

    private static StatementBlockSpacingSettings GetConfig(IFileSystem? fileSystem) =>
        ALCopsSettingsProvider.GetSettings(fileSystem).StatementBlockSpacing;

    private static bool IncludesExit(StatementBlockSpacingSettings config) =>
        config.ScopeLeavingMode is ScopeLeavingMode.ExitOnly or ScopeLeavingMode.ExitAndError;

    private static bool IncludesError(StatementBlockSpacingSettings config) =>
        config.ScopeLeavingMode is ScopeLeavingMode.ErrorOnly or ScopeLeavingMode.ExitAndError;

    private static bool IncludesOneLiners(StatementBlockSpacingSettings config) =>
        config.OneLinerMode == OneLinerMode.All;

    private static bool IsOneLiner(StatementSyntax statement)
    {
        var span = statement.GetLocation().GetLineSpan();

        return span.StartLinePosition.Line == span.EndLinePosition.Line;
    }

    private static bool IsBuiltInErrorInvocation(IInvocationExpression invocation)
    {
        if (invocation.TargetMethod is not IMethodSymbol targetMethod)
        {
            return false;
        }

        return targetMethod.MethodKind == EnumProvider.MethodKind.BuiltInMethod &&
            SemanticFacts.IsSameName(targetMethod.Name, ErrorMethodName);
    }

    private static bool IsControlFlowStatement(SyntaxNode node) =>
        ControlFlowStatementKinds.Contains(node.Kind);

    private static ImmutableArray<StatementSyntax> GetSiblingStatements(StatementSyntax statement)
    {
        if (statement.Parent is null)
        {
            return [];
        }

        return [.. statement.Parent.ChildNodes().OfType<StatementSyntax>()];
    }

    private static string GetControlFlowStatementName(SyntaxNode node)
    {
        if (node.IsKind(EnumProvider.SyntaxKind.IfStatement))
        {
            return "if";
        }

        if (node.IsKind(EnumProvider.SyntaxKind.CaseStatement))
        {
            return "case";
        }

        if (node.IsKind(EnumProvider.SyntaxKind.RepeatStatement))
        {
            return "repeat";
        }

        if (node.IsKind(EnumProvider.SyntaxKind.WhileStatement))
        {
            return "while";
        }

        if (node.IsKind(EnumProvider.SyntaxKind.ForStatement))
        {
            return "for";
        }

        if (node.IsKind(EnumProvider.SyntaxKind.ForEachStatement))
        {
            return "foreach";
        }

        return "control-flow";
    }

    private static void ReportMissingBlankLineBeforeIfNeeded(
        SyntaxNodeAnalysisContext ctx,
        SyntaxNode previousStatement,
        SyntaxToken currentToken,
        string requirement)
    {
        if (!HasBlankLineBetween(previousStatement.GetLastToken(), currentToken))
        {
            ReportDiagnostic(ctx, currentToken, requirement);
        }
    }

    private static void ReportMissingBlankLineBeforeIfNeeded(
        OperationAnalysisContext ctx,
        SyntaxNode previousStatement,
        SyntaxToken currentToken,
        string requirement)
    {
        if (!HasBlankLineBetween(previousStatement.GetLastToken(), currentToken))
        {
            ReportDiagnostic(ctx, currentToken, requirement);
        }
    }

    private static bool HasBlankLineBetween(SyntaxToken previousToken, SyntaxToken nextToken) =>
        nextToken.GetLocation().GetLineSpan().StartLinePosition.Line -
        previousToken.GetLocation().GetLineSpan().EndLinePosition.Line >= 2;

    private static void ReportDiagnostic(
        SyntaxNodeAnalysisContext ctx,
        SyntaxToken token,
        string requirement) =>
        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.StatementBlocksSeparatedByBlankLine,
            token.GetLocation(),
            requirement));

    private static void ReportDiagnostic(
        OperationAnalysisContext ctx,
        SyntaxToken token,
        string requirement) =>
        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.StatementBlocksSeparatedByBlankLine,
            token.GetLocation(),
            requirement));
}
