using ALCops.ApplicationCop.CodeFixes;
using RoslynTestKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace ALCops.ApplicationCop.Test
{
    public class IntegrationEventInInternalCodeunit : NavCodeAnalysisBase
    {
        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.IntegrationEventInInternalCodeunit _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.IntegrationEventInInternalCodeunit>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(IntegrationEventInInternalCodeunit)));
        }

        [Test]
        [TestCase("IntegrationEvent")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.IntegrationEventInInternalCodeunit);
        }

        [Test]
        [TestCase("IntegrationEventAccessPublic")]
        [TestCase("IntegrationEventCodeunitObsolete")]
        [TestCase("IntegrationEventMethodObsolete")]
        [TestCase("InternalEvent")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.IntegrationEventInInternalCodeunit);
        }

        [Test]
        [TestCase("ConvertToInternalEvent")]
        [TestCase("RemoveAccessInternal")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var config = new CodeFixTestFixtureConfig
            {
                AdditionalAnalyzers = [_analyzer]
            };

            if (testCase == "ConvertToInternalEvent")
            {
                var fixture = RoslynFixtureFactory.Create<IntegrationEventInInternalCodeunitConvertToInternalEventFixProvider>(config);
                fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.IntegrationEventInInternalCodeunit);
            }
            else if (testCase == "RemoveAccessInternal")
            {
                var fixture = RoslynFixtureFactory.Create<IntegrationEventInInternalCodeunitRemoveAccessInternalFixProvider>(config);
                fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.IntegrationEventInInternalCodeunit);
            }
            else
            {
                Assert.Fail($"Unknown test case: {testCase}");
            }
        }
    }
}