using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.Common.Diagnostics;

/// <summary>
/// A <see cref="CompilationStartAnalysisContext"/> decorator that wraps the
/// nested registrations made inside a <c>RegisterCompilationStartAction</c>
/// callback. Because every registration method on
/// <see cref="CompilationStartAnalysisContext"/> is public-abstract, these
/// overrides intercept the nested callbacks via virtual dispatch even when the
/// analyzer's callback variable is typed as the SDK base type - so no call-site
/// changes are needed in analyzers that use <c>RegisterCompilationStartAction</c>.
/// </summary>
public sealed class SafeCompilationStartContext : CompilationStartAnalysisContext
{
    private readonly CompilationStartAnalysisContext _inner;
    private readonly DiagnosticDescriptor _descriptor;
    private readonly string _analyzerName;

    internal SafeCompilationStartContext(
        CompilationStartAnalysisContext inner,
        DiagnosticDescriptor descriptor,
        string analyzerName)
        : base(inner.Compilation, inner.Options, inner.CancellationToken)
    {
        _inner = inner;
        _descriptor = descriptor;
        _analyzerName = analyzerName;
    }

    public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds) =>
        _inner.RegisterSymbolAction(
            ctx =>
            {
                try { action(ctx); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    ctx.ReportDiagnostic(AnalyzerExceptionReporter.CreateDiagnostic(
                        _descriptor, ctx.Symbol.GetLocation(), _analyzerName, ex));
                }
            },
            symbolKinds);

    public override void RegisterSyntaxNodeAction(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<SyntaxKind> syntaxKinds) =>
        _inner.RegisterSyntaxNodeAction(
            ctx =>
            {
                try { action(ctx); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    ctx.ReportDiagnostic(AnalyzerExceptionReporter.CreateDiagnostic(
                        _descriptor, ctx.Node.GetLocation(), _analyzerName, ex));
                }
            },
            syntaxKinds);

    public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action) =>
        _inner.RegisterCodeBlockAction(
            ctx =>
            {
                try { action(ctx); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    ctx.ReportDiagnostic(AnalyzerExceptionReporter.CreateDiagnostic(
                        _descriptor, ctx.CodeBlock.GetLocation(), _analyzerName, ex));
                }
            });

    public override void RegisterCompilationEndAction(Action<CompilationAnalysisContext> action) =>
        _inner.RegisterCompilationEndAction(
            ctx =>
            {
                try { action(ctx); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    ctx.ReportDiagnostic(AnalyzerExceptionReporter.CreateDiagnostic(
                        _descriptor, Location.None, _analyzerName, ex));
                }
            });

    public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action) =>
        _inner.RegisterSemanticModelAction(
            ctx =>
            {
                try { action(ctx); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    ctx.ReportDiagnostic(AnalyzerExceptionReporter.CreateDiagnostic(
                        _descriptor, Location.None, _analyzerName, ex));
                }
            });

    public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action) =>
        _inner.RegisterSyntaxTreeAction(
            ctx =>
            {
                try { action(ctx); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    ctx.ReportDiagnostic(AnalyzerExceptionReporter.CreateDiagnostic(
                        _descriptor, Location.None, _analyzerName, ex));
                }
            });

    // CodeBlockStart exposes its own start context with nested registrations. No
    // ALCops analyzer uses it today; forward unwrapped so it stays functional.
    public override void RegisterCodeBlockStartAction(Action<CodeBlockStartAnalysisContext> action) =>
        _inner.RegisterCodeBlockStartAction(action);
}
