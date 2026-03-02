using System.Collections.Concurrent;
using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class AccessPropertyExplicitlySet : DiagnosticAnalyzer
{
    private static readonly ConcurrentDictionary<Compilation, bool> EnumOrInterfaceAccessSupportedCache = new();

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.AccessPropertyExplicitlySet);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            this.AnalyzeAccessProperty,
            EnumProvider.SymbolKind.Codeunit,
            EnumProvider.SymbolKind.Enum,
            EnumProvider.SymbolKind.Interface,
            EnumProvider.SymbolKind.PermissionSet,
            EnumProvider.SymbolKind.Query,
            EnumProvider.SymbolKind.Table
        );

    private void AnalyzeAccessProperty(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        var kind = ctx.Symbol.Kind;

        if (kind == EnumProvider.SymbolKind.Enum ||
            kind == EnumProvider.SymbolKind.Interface)
        {
            if (!IsEnumOrInterfaceAccessSupported(ctx.Compilation))
                return;
        }

        if (ctx.Symbol.GetProperty(EnumProvider.PropertyKind.Access) is not null)
            return;

        ctx.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.AccessPropertyExplicitlySet,
                ctx.Symbol.GetLocation(),
                ctx.Symbol.Kind.ToString(),
                ctx.Symbol.Name));
    }

    private static bool IsEnumOrInterfaceAccessSupported(Compilation compilation)
        => EnumOrInterfaceAccessSupportedCache.GetOrAdd(compilation, static c =>
        {
            var manifest = ManifestHelper.GetManifest(c);
            return manifest is not null && manifest.Runtime >= RuntimeVersion.Spring2021;
        });
}