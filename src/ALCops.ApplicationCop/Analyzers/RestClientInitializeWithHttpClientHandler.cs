using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class RestClientInitializeWithHttpClientHandler : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.RestClientInitializeWithHttpClientHandler);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeObject,
            EnumProvider.SymbolKind.Codeunit,
            EnumProvider.SymbolKind.Table,
            EnumProvider.SymbolKind.TableExtension,
            EnumProvider.SymbolKind.Page,
            EnumProvider.SymbolKind.PageExtension,
            EnumProvider.SymbolKind.Report,
            EnumProvider.SymbolKind.ReportExtension,
            EnumProvider.SymbolKind.Query,
            EnumProvider.SymbolKind.XmlPort);

    private static void AnalyzeObject(SymbolAnalysisContext ctx)
    {
    }
}
