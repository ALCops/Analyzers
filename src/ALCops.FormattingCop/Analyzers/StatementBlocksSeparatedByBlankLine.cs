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

    // Single source of truth for control-flow kinds and their user-facing names. Consumed by
    // the syntax-node registration, IsControlFlowStatement(), and GetControlFlowStatementName().
    // Adding a new kind here is enough.
    private static readonly Dictionary<SyntaxKind, string> ControlFlowStatementNames = new()
    {
        [EnumProvider.SyntaxKind.IfStatement]      = "if",
        [EnumProvider.SyntaxKind.CaseStatement]    = "case",
        [EnumProvider.SyntaxKind.RepeatStatement]  = "repeat",
        [EnumProvider.SyntaxKind.WhileStatement]   = "while",
        [EnumProvider.SyntaxKind.ForStatement]     = "for",
        [EnumProvider.SyntaxKind.ForEachStatement] = "foreach",
    };

    private static readonly SyntaxKind[] ControlFlowStatementKindsArray =
        ControlFlowStatementNames.Keys.ToArray();

    private static readonly ImmutableHashSet<SyntaxKind> ControlFlowStatementKinds =
        ImmutableHashSet.CreateRange(ControlFlowStatementNames.Keys);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeControlFlowNode, ControlFlowStatementKindsArray);

        context.RegisterSyntaxNodeAction(
            AnalyzeExitStatement,
            EnumProvider.SyntaxKind.ExitStatement);

        context.RegisterOperationAction(
            AnalyzeErrorInvocation,
            EnumProvider.OperationKind.InvocationExpression);
    }

    private void AnalyzeControlFlowNode(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not StatementSyntax statement)
        {
            return;
        }

        var config = GetConfig(ctx.SemanticModel.Compilation.FileSystem);

        AnalyzeControlFlowStatement(ctx.ReportDiagnostic, statement, config);
        AnalyzeElseChain(ctx.ReportDiagnostic, statement, config);
    }

    private static void AnalyzeControlFlowStatement(
        Action<Diagnostic> report,
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

        if (!TryGetSiblingIndex(statement, out var siblings, out var i))
        {
            return;
        }

        var name = GetControlFlowStatementName(statement);

        if (config.ControlFlowBefore && i > 0)
        {
            ReportIfNoBlankLineBetween(
                report,
                siblings[i - 1].GetLastToken(),
                statement.GetFirstToken(),
                $"before '{name}' block");
        }

        if (config.ControlFlowAfter && i < siblings.Length - 1 && !IsControlFlowStatement(siblings[i + 1]))
        {
            ReportIfNoBlankLineBetween(
                report,
                statement.GetLastToken(),
                siblings[i + 1].GetFirstToken(),
                $"after '{name}' block");
        }
    }

    private static void AnalyzeElseChain(
        Action<Diagnostic> report,
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

        ReportIfNoBlankLineBetween(report, tokenBeforeElse, elseToken, "before 'else' keyword");
    }

    private void AnalyzeExitStatement(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not StatementSyntax statement)
        {
            return;
        }

        // Direct else/then branch statements are not block siblings; comparing them against the
        // sibling branch produces false positives (`end else exit;`, `... else exit;`).
        if (statement.Parent is IfStatementSyntax)
        {
            return;
        }

        var config = GetConfig(ctx.SemanticModel.Compilation.FileSystem);

        if (!IncludesExit(config))
        {
            return;
        }

        CheckScopeLeavingSpacing(ctx.ReportDiagnostic, statement, "before scope-leaving statement 'exit'");
    }

    private void AnalyzeErrorInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
        {
            return;
        }

        if (invocation.Syntax.Parent is not ExpressionStatementSyntax expressionStatement)
        {
            return;
        }

        // Direct else/then branch statements are not block siblings; comparing them against the
        // sibling branch produces false positives (`end else Error(...);`).
        if (expressionStatement.Parent is IfStatementSyntax)
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

        CheckScopeLeavingSpacing(ctx.ReportDiagnostic, expressionStatement, "before scope-leaving statement 'Error()'");
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
            return ImmutableArray<StatementSyntax>.Empty;
        }

        return statement.Parent.ChildNodes().OfType<StatementSyntax>().ToImmutableArray();
    }

    private static string GetControlFlowStatementName(SyntaxNode node) =>
        ControlFlowStatementNames.TryGetValue(node.Kind, out var name) ? name : "control-flow";

    private static bool TryGetSiblingIndex(
        StatementSyntax statement,
        out ImmutableArray<StatementSyntax> siblings,
        out int index)
    {
        siblings = GetSiblingStatements(statement);

        for (int i = 0; i < siblings.Length; i++)
        {
            if (siblings[i] == statement)
            {
                index = i;

                return true;
            }
        }

        index = -1;

        return false;
    }

    private static void CheckScopeLeavingSpacing(
        Action<Diagnostic> report,
        StatementSyntax statement,
        string requirement)
    {
        if (!TryGetSiblingIndex(statement, out var siblings, out var i) || i == 0)
        {
            return;
        }

        ReportIfNoBlankLineBetween(
            report,
            siblings[i - 1].GetLastToken(),
            statement.GetFirstToken(),
            requirement);
    }

    private static void ReportIfNoBlankLineBetween(
        Action<Diagnostic> report,
        SyntaxToken leading,
        SyntaxToken trailing,
        string requirement)
    {
        if (!HasBlankLineBetween(leading, trailing))
        {
            report(Diagnostic.Create(
                DiagnosticDescriptors.StatementBlocksSeparatedByBlankLine,
                trailing.GetLocation(),
                requirement));
        }
    }

    private static bool HasBlankLineBetween(SyntaxToken previousToken, SyntaxToken nextToken) =>
        nextToken.GetLocation().GetLineSpan().StartLinePosition.Line -
        previousToken.GetLocation().GetLineSpan().EndLinePosition.Line >= 2;
}
