using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class RequiredInterfaceImplementation : DiagnosticAnalyzer
{
    // A variable of one of the trigger codeunits (matched on object id and name) requires a codeunit
    // declared in the compiling app that implements the named interface.
    private static readonly Pair[] Pairs =
    [
        new Pair(
            DiagnosticDescriptors.RestClientRequiresHttpClientHandler,
            [(2350, "Rest Client")],
            "Http Client Handler"),
        new Pair(
            DiagnosticDescriptors.TelemetryRequiresTelemetryLogger,
            [(8711, "Telemetry"), (8703, "Feature Telemetry")],
            "Telemetry Logger"),
    ];

    // Member kinds whose own members can declare code (triggers, nested controls, actions, dataitems, nodes).
    // Keys are left out on purpose: in a tableextension they enumerate the target table's fields, which would
    // attribute base-table field triggers to the extension.
    private static readonly ImmutableHashSet<SymbolKind> DescendKinds = ImmutableHashSet.Create(
        EnumProvider.SymbolKind.Field,
        EnumProvider.SymbolKind.Control,
        EnumProvider.SymbolKind.Action,
        EnumProvider.SymbolKind.Change,
        EnumProvider.SymbolKind.ReportDataItem,
        EnumProvider.SymbolKind.XmlPortNode,
        EnumProvider.SymbolKind.RequestPage,
        EnumProvider.SymbolKind.RequestPageExtension);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.RestClientRequiresHttpClientHandler,
            DiagnosticDescriptors.TelemetryRequiresTelemetryLogger);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        Compilation compilation = context.Compilation;
        CancellationToken cancellationToken = context.CancellationToken;

        // Read-only per-compilation index, built on first use by whichever object needs it.
        var implemented = new Lazy<bool[]>(
            () => BuildImplementedIndex(compilation, cancellationToken),
            LazyThreadSafetyMode.ExecutionAndPublication);

        context.RegisterSymbolAction(
            ctx => AnalyzeObject(ctx, implemented),
            EnumProvider.SymbolKind.Codeunit,
            EnumProvider.SymbolKind.Table,
            EnumProvider.SymbolKind.TableExtension,
            EnumProvider.SymbolKind.Page,
            EnumProvider.SymbolKind.PageExtension,
            EnumProvider.SymbolKind.Report,
            EnumProvider.SymbolKind.ReportExtension,
            EnumProvider.SymbolKind.Query,
            EnumProvider.SymbolKind.XmlPort);
    }

    private static void AnalyzeObject(SymbolAnalysisContext ctx, Lazy<bool[]> implemented)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Symbol is not IContainerSymbol container)
            return;

        if (ctx.Symbol is ICodeunitTypeSymbol codeunit &&
            (codeunit.Subtype == EnumProvider.CodeunitSubtypeKind.Test ||
             codeunit.Subtype == EnumProvider.CodeunitSubtypeKind.TestRunner))
            return;

        var used = new bool[Pairs.Length];
        CollectUsedPairs(container, used, ctx.CancellationToken);

        for (int i = 0; i < Pairs.Length; i++)
        {
            if (!used[i] || implemented.Value[i])
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Pairs[i].Descriptor,
                ctx.Symbol.GetLocation(),
                ctx.Symbol.Kind.ToString(),
                ctx.Symbol.Name));
        }
    }

    private static bool[] BuildImplementedIndex(Compilation compilation, CancellationToken cancellationToken)
    {
        var implemented = new bool[Pairs.Length];

        // Declared symbols of the compiling module only: implementations in dependencies do not count.
        foreach (ICodeunitTypeSymbol codeunit in compilation.GetDeclaredApplicationObjectSymbols().OfType<ICodeunitTypeSymbol>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var implementedInterface in codeunit.ImplementedInterfaces)
            {
                for (int i = 0; i < Pairs.Length; i++)
                {
                    if (SemanticFacts.IsSameName(implementedInterface.Name, Pairs[i].InterfaceName))
                        implemented[i] = true;
                }
            }
        }

        return implemented;
    }

    // One walk over the object marks every pair whose trigger codeunit is declared somewhere in it and stops
    // as soon as all pairs are marked. GetMembers returns the members declared on the container itself
    // (globals, procedures, triggers, layout controls, actions, dataitems, nodes, changes); members of a base
    // or related table are not included.
    private static bool CollectUsedPairs(IContainerSymbol container, bool[] used, CancellationToken cancellationToken)
    {
        foreach (ISymbol member in container.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool allUsed = member switch
            {
                IVariableSymbol variable => MarkTriggerCodeunit(variable.Type, used),
                IMethodSymbol method => MarkMethod(method, used),
                IContainerSymbol nested when DescendKinds.Contains(member.Kind) => CollectUsedPairs(nested, used, cancellationToken),
                _ => false,
            };

            if (allUsed)
                return true;
        }

        return false;
    }

    private static bool MarkMethod(IMethodSymbol method, bool[] used)
    {
        foreach (IVariableSymbol local in method.LocalVariables)
        {
            if (MarkTriggerCodeunit(local.Type, used))
                return true;
        }

        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (MarkTriggerCodeunit(parameter.ParameterType, used))
                return true;
        }

        return MarkTriggerCodeunit(method.ReturnValueSymbol?.ReturnType, used);
    }

    // Marks the pair whose trigger codeunit matches the type; returns true once every pair is marked.
    // The type of a codeunit variable is the codeunit symbol itself; unresolved types fail the cast.
    private static bool MarkTriggerCodeunit(ITypeSymbol? type, bool[] used)
    {
        if (type is null || type.NavTypeKind != EnumProvider.NavTypeKind.Codeunit)
            return false;

        if ((type.OriginalDefinition as ICodeunitTypeSymbol ?? type as ICodeunitTypeSymbol) is not { } codeunit)
            return false;

        for (int i = 0; i < Pairs.Length; i++)
        {
            if (used[i])
                continue;

            foreach ((int id, string name) in Pairs[i].TriggerCodeunits)
            {
                if (codeunit.Id == id && SemanticFacts.IsSameName(codeunit.Name, name))
                {
                    used[i] = true;
                    break;
                }
            }
        }

        return Array.TrueForAll(used, static u => u);
    }

    private sealed class Pair(
        DiagnosticDescriptor descriptor,
        (int Id, string Name)[] triggerCodeunits,
        string interfaceName)
    {
        public DiagnosticDescriptor Descriptor { get; } = descriptor;
        public (int Id, string Name)[] TriggerCodeunits { get; } = triggerCodeunits;
        public string InterfaceName { get; } = interfaceName;
    }
}
