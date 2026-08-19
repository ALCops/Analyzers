using System.Collections.Immutable;
using System.Text;
using ALCops.Common.Extensions;
using ALCops.Common.Helpers;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class InterfaceObjectNameGuide : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.InterfaceObjectNameGuide);

    private static readonly char CharOfCapitalI = 'I';

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(startContext =>
        {
            // Resolve the affixes once per compilation and capture them in the closure;
            // no static state, so concurrent compilations cannot observe each other's affixes.
            var affixes = new Lazy<string[]>(
                () => MandatoryAffixes.GetAffixes(startContext.Compilation),
                LazyThreadSafetyMode.ExecutionAndPublication);

            startContext.RegisterSymbolAction(
                ctx => AnalyzeObjectName(ctx, affixes),
                EnumProvider.SymbolKind.Interface);
        });
    }

    private static void AnalyzeObjectName(SymbolAnalysisContext ctx, Lazy<string[]> affixes)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IInterfaceTypeSymbol interfaceTypeSymbol)
            return;

        // The interface object should start with a capital 'I' and should not have a space after it
        if (interfaceTypeSymbol.Name.StartsWith(CharOfCapitalI) && !char.IsWhiteSpace(interfaceTypeSymbol.Name[1]))
            return;

        int? indexAfterAffix = MandatoryAffixes.GetIndexAfterLeadingAffix(interfaceTypeSymbol.Name, affixes.Value);
        if (indexAfterAffix is null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InterfaceObjectNameGuide,
                interfaceTypeSymbol.GetLocation(),
                interfaceTypeSymbol.Name));

            return;
        }

        string objectNameWithoutPrefix = interfaceTypeSymbol.Name.Remove(0, indexAfterAffix.GetValueOrDefault());

        // The first character after the prefix should be a capital 'I'
        if (RemoveSpecialCharacters(objectNameWithoutPrefix)[0] != CharOfCapitalI)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InterfaceObjectNameGuide,
                interfaceTypeSymbol.GetLocation(),
                interfaceTypeSymbol.Name));

            return;
        }

        // The character after the capital 'I' should not be a whitespace
        int index = objectNameWithoutPrefix.IndexOf(CharOfCapitalI);
        if (index != -1 && index < objectNameWithoutPrefix.Length - 1)
        {
            if (char.IsWhiteSpace(objectNameWithoutPrefix[index + 1]))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InterfaceObjectNameGuide,
                    interfaceTypeSymbol.GetLocation(),
                    interfaceTypeSymbol.Name));

                return;
            }
        }
    }

    private static string RemoveSpecialCharacters(string str)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in str)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}