using System.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.Test.Conventions;

/// <summary>
/// Source-linked into every cop test project to check the documentation paths users receive.
/// </summary>
public sealed class DiagnosticHelpUrls
{
    [Test]
    public void EveryDiagnosticHelpUrlHasLowercaseDocumentationPath()
    {
        var testAssemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
        var copAssemblyName = testAssemblyName[..^".Test".Length];
        var copAssembly = Assembly.Load(copAssemblyName);
        var descriptorType = copAssembly.GetType($"{copAssemblyName}.DiagnosticDescriptors", throwOnError: true)!;
        var descriptors = descriptorType.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(DiagnosticDescriptor))
            .Select(field => (DiagnosticDescriptor)field.GetValue(null)!)
            .ToArray();

        Assert.That(descriptors, Is.Not.Empty);

        var copSlug = copAssemblyName["ALCops.".Length..].ToLowerInvariant();
        foreach (var descriptor in descriptors)
        {
            var uri = new Uri(descriptor.HelpLinkUri, UriKind.Absolute);
            Assert.That(uri.AbsolutePath,
                Is.EqualTo($"/docs/analyzers/{copSlug}/{descriptor.Id.ToLowerInvariant()}/"),
                $"{copAssemblyName}: {descriptor.Id}");
        }
    }
}
