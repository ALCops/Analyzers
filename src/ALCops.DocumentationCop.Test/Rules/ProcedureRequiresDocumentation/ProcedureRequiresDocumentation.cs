using RoslynTestKit;

namespace ALCops.DocumentationCop.Test
{
	public class ProcedureRequiresDocumentation : NavCodeAnalysisBase
	{
		private AnalyzerTestFixture _fixture;
		private string _testCasePath;

		[SetUp]
		public void Setup()
		{
			_testCasePath = Path.Combine(
				Directory.GetParent(
					Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
					Path.Combine("Rules", nameof(ProcedureRequiresDocumentation)));

		    _fixture = RoslynFixtureFactory.Create<Analyzers.ProcedureRequiresDocumentation>(
				// Inject a ruleset to enable testing for rules, that are not enabled by default (isEnabledByDefault: false).
				new AnalyzerTestFixtureConfig
				{
					RuleSetPath = Path.Combine(_testCasePath, $"{nameof(ProcedureRequiresDocumentation)}.ruleset.json")
				});
		}

		[Test]
		[TestCase("BusinessEvent")]
		[TestCase("BusinessEventWithComment")]
		[TestCase("BusinessEventWithParameters")]
		[TestCase("IntegrationEvent")]
		[TestCase("IntegrationEventWithComment")]
		[TestCase("IntegrationEventWithParameters")]
		public async Task IntegrationEventHasDiagnostic(string testCase)
		{
			var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(IntegrationEventHasDiagnostic), $"{testCase}.al"))
				.ConfigureAwait(false);

			_fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.EventRequiresDocumentation);
		}

		[Test]
		[TestCase("BusinessEvent")]
		[TestCase("BusinessEventWithParameters")]
		[TestCase("IntegrationEvent")]
		[TestCase("IntegrationEventWithParameters")]
		public async Task IntegrationEventNoDiagnostic(string testCase)
		{
			var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(IntegrationEventNoDiagnostic), $"{testCase}.al"))
				.ConfigureAwait(false);

			_fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.EventRequiresDocumentation);
		}

		[Test]
		[TestCase("InternalEvent")]
		[TestCase("InternalEventWithComment")]
		[TestCase("InternalEventWithParameters")]
		public async Task InternalEventHasDiagnostic(string testCase)
		{
			var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(InternalEventHasDiagnostic), $"{testCase}.al"))
				.ConfigureAwait(false);

			_fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.InternalEventRequiresDocumentation);
		}

		[Test]
		[TestCase("InternalEvent")]
		[TestCase("InternalEventWithParameters")]
		public async Task InternalEventNoDiagnostic(string testCase)
		{
			var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(InternalEventNoDiagnostic), $"{testCase}.al"))
				.ConfigureAwait(false);

			_fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.InternalEventRequiresDocumentation);
		}

		//-----------------------
		[Test]
		[TestCase("CodeunitAccessInternal")]
		[TestCase("CodeunitAccessInternalProcedureWithAttribute")]
		[TestCase("CodeunitAccessInternalProcedureWithComment")]
		[TestCase("InternalProcedure")]
		[TestCase("InternalProcedureWithAttribute")]
		[TestCase("InternalProcedureWithComment")]
		public async Task InternalHasDiagnostic(string testCase)
		{
			var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(InternalHasDiagnostic), $"{testCase}.al"))
				.ConfigureAwait(false);

			_fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.InternalProcedureRequiresDocumentation);
		}

		[Test]
		[TestCase("CodeunitAccessInternalProcedureDocumentationComment")]
		[TestCase("CodeunitAccessInternalProcedureDocumentationCommentWithAttribute")]
		[TestCase("CodeunitAccessInternalProcedureDocumentationCommentWithMultipleAttributes")]
		[TestCase("Procedure")]
		[TestCase("ProcedureLocal")]
		[TestCase("TestCodeunit")]
		[TestCase("TestCodeunitHandlerMethod")]
		public async Task InternalNoDiagnostic(string testCase)
		{
			var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(InternalNoDiagnostic), $"{testCase}.al"))
				.ConfigureAwait(false);

			_fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.InternalProcedureRequiresDocumentation);
		}

		[Test]
		[TestCase("Procedure")]
		[TestCase("ProcedureWithAttribute")]
		[TestCase("ProcedureWithComment")]
		public async Task PublicHasDiagnostic(string testCase)
		{
			var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(PublicHasDiagnostic), $"{testCase}.al"))
				.ConfigureAwait(false);

			_fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.PublicProcedureRequiresDocumentation);
		}

		[Test]
		[TestCase("CodeunitAccessInternal")]
		[TestCase("ProcedureDocumentationComment")]
		[TestCase("ProcedureDocumentationCommentWithAttribute")]
		[TestCase("ProcedureDocumentationCommentWithMultipleAttributes")]
		[TestCase("ProcedureInternal")]
		[TestCase("ProcedureLocal")]
		[TestCase("TestCodeunit")]
		[TestCase("TestCodeunitHandlerMethod")]
		public async Task PublicNoDiagnostic(string testCase)
		{
			var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(PublicNoDiagnostic), $"{testCase}.al"))
				.ConfigureAwait(false);

			_fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.PublicProcedureRequiresDocumentation);
		}
	}
}
