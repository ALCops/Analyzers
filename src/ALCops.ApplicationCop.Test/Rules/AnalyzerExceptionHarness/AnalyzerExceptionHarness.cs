using ALCops.Common.Diagnostics;
using ALCops.Common.Reflection;
using ALCops.ApplicationCop;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    /// <summary>
    /// Exercises the shared analyzer-exception harness (XX0000) via test-only
    /// analyzers that deliberately throw from each registration surface the harness
    /// wraps: a symbol action (matches CaptionRequired), an operation action (the
    /// <c>new</c>-hiding path), and a CompilationStart-nested symbol action. Each
    /// asserts AC0000 is reported at the analyzed object/line instead of AD0001.
    /// </summary>
    public class AnalyzerExceptionHarness : NavCodeAnalysisBase
    {
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(AnalyzerExceptionHarness)));
        }

        [Test]
        public async Task SymbolAction()
        {
            var fixture = RoslynFixtureFactory.Create<ThrowingSymbolAnalyzer>();
            var code = await ReadCaseAsync(nameof(SymbolAction)).ConfigureAwait(false);
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.AnalyzerException);
        }

        [Test]
        public async Task OperationAction()
        {
            var fixture = RoslynFixtureFactory.Create<ThrowingOperationAnalyzer>();
            var code = await ReadCaseAsync(nameof(OperationAction)).ConfigureAwait(false);
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.AnalyzerException);
        }

        [Test]
        public async Task CompilationStartAction()
        {
            var fixture = RoslynFixtureFactory.Create<ThrowingCompilationStartAnalyzer>();
            var code = await ReadCaseAsync(nameof(CompilationStartAction)).ConfigureAwait(false);
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.AnalyzerException);
        }

        private Task<string> ReadCaseAsync(string testCase) =>
            File.ReadAllTextAsync(
                Path.Combine(_testCasePath, "HasDiagnostic", $"{testCase}.al"));
    }

    [DiagnosticAnalyzer]
    public sealed class ThrowingSymbolAnalyzer : ApplicationCopAnalyzer
    {
        protected override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCore { get; } =
            System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor>.Empty;

        protected override void InitializeAnalyzer(SafeAnalysisContext context) =>
            context.RegisterSymbolAction(
                _ => throw new InvalidOperationException("boom"),
                EnumProvider.SymbolKind.Table);
    }

    [DiagnosticAnalyzer]
    public sealed class ThrowingOperationAnalyzer : ApplicationCopAnalyzer
    {
        protected override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCore { get; } =
            System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor>.Empty;

        protected override void InitializeAnalyzer(SafeAnalysisContext context) =>
            context.RegisterOperationAction(
                _ => throw new InvalidOperationException("boom"),
                EnumProvider.OperationKind.InvocationExpression);
    }

    [DiagnosticAnalyzer]
    public sealed class ThrowingCompilationStartAnalyzer : ApplicationCopAnalyzer
    {
        protected override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCore { get; } =
            System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor>.Empty;

        protected override void InitializeAnalyzer(SafeAnalysisContext context) =>
            context.RegisterCompilationStartAction(
                start => start.RegisterSymbolAction(
                    _ => throw new InvalidOperationException("boom"),
                    EnumProvider.SymbolKind.Table));
    }
}
