using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.Common.Diagnostics;

/// <summary>
/// An <see cref="AnalysisContext"/> decorator that forwards every registration to
/// the real context but wraps each callback in a try/catch. When a callback throws
/// an unhandled exception (other than cancellation), it is reported as a located
/// <c>XX0000</c> diagnostic instead of bubbling up to the SDK as <c>AD0001</c>.
///
/// Note on the SDK coupling: this type subclasses the SDK's abstract
/// <see cref="AnalysisContext"/>. If a future SDK version adds a new public-abstract
/// <c>Register*</c> method, this class will fail to compile until an override is
/// added here. That compile-time break is intentional - it forces wrapping of any
/// new registration surface.
/// </summary>
public sealed class SafeAnalysisContext : AnalysisContext
{
    private readonly AnalysisContext _inner;
    private readonly DiagnosticDescriptor _descriptor;
    private readonly string _analyzerName;

    internal SafeAnalysisContext(AnalysisContext inner, DiagnosticDescriptor descriptor, string analyzerName)
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

    public override void RegisterCompilationAction(Action<CompilationAnalysisContext> action) =>
        _inner.RegisterCompilationAction(
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

    public override void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action) =>
        _inner.RegisterCompilationStartAction(
            startContext => action(new SafeCompilationStartContext(startContext, _descriptor, _analyzerName)));

    // CodeBlockStart exposes its own start context with nested registrations. No
    // ALCops analyzer uses it today; forward unwrapped so it stays functional.
    public override void RegisterCodeBlockStartAction(Action<CodeBlockStartAnalysisContext> action) =>
        _inner.RegisterCodeBlockStartAction(action);

    // Operation registration cannot be intercepted through the public abstract
    // surface (the params overload routes through internal members we cannot
    // override). These 'new' overloads win because analyzers reference the
    // SafeAnalysisContext type, and they forward to the inner context's public
    // params overload, which the real context implements.
    public new void RegisterOperationAction(Action<OperationAnalysisContext> action, params OperationKind[] operationKinds) =>
        _inner.RegisterOperationAction(WrapOperation(action), operationKinds);

    public new void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds) =>
        _inner.RegisterOperationAction(WrapOperation(action), operationKinds.ToArray());

    private Action<OperationAnalysisContext> WrapOperation(Action<OperationAnalysisContext> action) =>
        ctx =>
        {
            try { action(ctx); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                ctx.ReportDiagnostic(AnalyzerExceptionReporter.CreateDiagnostic(
                    _descriptor, ctx.Operation.Syntax.GetLocation(), _analyzerName, ex));
            }
        };
}
