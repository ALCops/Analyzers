using System.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.Test.Conventions;

/// <summary>
/// Every concrete analyzer, CodeFixProvider and CodeAction is sealed: the host instantiates them via attributes and
/// never subclasses them. CA1852 enforces this only for non-public types in assemblies without InternalsVisibleTo,
/// so this test covers the rest. The file is source-linked into every ALCops.*.Test project and inspects the
/// assembly the test project is named after (ALCops.X.Test -> ALCops.X).
/// </summary>
public sealed class AnalyzerTypesAreSealed
{
    private static readonly Type[] LeafBaseTypes = [typeof(DiagnosticAnalyzer), typeof(CodeFixProvider), typeof(CodeAction)];

    [Test]
    public void EveryConcreteAnalyzerCodeFixAndCodeActionIsSealed()
    {
        var testAssemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
        var copAssembly = Assembly.Load(testAssemblyName[..^".Test".Length]);

        var unsealed = copAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsSealed)
            .Where(t => LeafBaseTypes.Any(b => b.IsAssignableFrom(t)))
            .Select(t => t.FullName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.That(unsealed, Is.Empty, $"Seal these types in {copAssembly.GetName().Name}: {string.Join(", ", unsealed)}");
    }
}
