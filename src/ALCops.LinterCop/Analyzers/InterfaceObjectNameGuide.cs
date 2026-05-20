using System.Collections.Immutable;
using System.Text;
using ALCops.Common.Extensions;
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

    private static IEnumerable<string>? Affixes = null;
    private static readonly char CharOfCapitalI = 'I';

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(
            this.PopulateListOfAffixes);

        context.RegisterSymbolAction(
            this.AnalyzeObjectName,
            EnumProvider.SymbolKind.Interface);
    }

    private void AnalyzeObjectName(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IInterfaceTypeSymbol interfaceTypeSymbol)
            return;

        // The interface object should start with a capital 'I' and should not have a space after it
        if (interfaceTypeSymbol.Name.StartsWith(CharOfCapitalI) && !char.IsWhiteSpace(interfaceTypeSymbol.Name[1]))
            return;

        int? indexAfterAffix = GetIndexAfterAffix(interfaceTypeSymbol.Name);
        if (indexAfterAffix is null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InterfaceObjectNameGuide,
                interfaceTypeSymbol.GetLocation()));

            return;
        }

        string objectNameWithoutPrefix = interfaceTypeSymbol.Name.Remove(0, indexAfterAffix.GetValueOrDefault());

        // The first character after the prefix should be a capital 'I'
        if (RemoveSpecialCharacters(objectNameWithoutPrefix)[0] != CharOfCapitalI)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InterfaceObjectNameGuide,
                interfaceTypeSymbol.GetLocation()));

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
                    interfaceTypeSymbol.GetLocation()));

                return;
            }
        }
    }

    private void PopulateListOfAffixes(CompilationStartAnalysisContext context)
    {
        Affixes = GetAffixes(context.Compilation);
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

    private static int? GetIndexAfterAffix(string typeSymbolName)
    {
        foreach (var affix in Affixes ?? Enumerable.Empty<string>())
        {
            if (typeSymbolName.StartsWith(affix, SemanticFacts.NameEqualityComparison))
            {
                int affixLength = affix.Length;
                if (typeSymbolName.Length > affixLength)
                {
                    return affixLength;
                }
            }
        }

        // Return null if no affix is found or no character is present after the affix
        return null;
    }

    private static List<string>? GetAffixes(Compilation compilation)
    {
        AppSourceCopConfiguration? copConfiguration = AppSourceCopConfigurationProvider.GetAppSourceCopConfiguration(compilation);
        if (copConfiguration is null)
            return null;

        List<string> affixes = new List<string>();
        if (!string.IsNullOrEmpty(copConfiguration.MandatoryPrefix) && !affixes.Contains(copConfiguration.MandatoryPrefix, StringComparer.OrdinalIgnoreCase))
            affixes.Add(copConfiguration.MandatoryPrefix);

        if (copConfiguration.MandatoryAffixes is not null)
        {
            foreach (string mandatoryAffix in copConfiguration.MandatoryAffixes)
            {
                if (!string.IsNullOrEmpty(mandatoryAffix) && !affixes.Contains(mandatoryAffix, StringComparer.OrdinalIgnoreCase))
                    affixes.Add(mandatoryAffix);
            }
        }
        return affixes;
    }
}