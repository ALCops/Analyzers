using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class LabelTokenRequiresTokSuffix : DiagnosticAnalyzer
{
    private const string TokSuffix = "Tok";

    // https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/analyzers/codecop-aa0074#remarks
    private static readonly string[] ApprovedSuffixes =
    {
        "Msg",
        "Tok",
        "Err",
        "Qst",
        "Lbl",
        "Txt"
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.LabelTokenRequiresTokSuffix);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeLockedLabel,
            EnumProvider.SymbolKind.GlobalVariable,
            EnumProvider.SymbolKind.LocalVariable);

    private static void AnalyzeLockedLabel(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IVariableSymbol variable)
            return;

        if (variable.Type is not ILabelTypeSymbol label)
            return;

        if (label.Locked is not true)
            return;

        string name = label.Name;
        if (name.Length >= 3 && name.EndsWith(TokSuffix, StringComparison.Ordinal))
            return;

        string? text = label.Text;
        if (string.IsNullOrEmpty(text))
            return;

        ReadOnlySpan<char> nameNoSuffix = GetNameWithoutApprovedSuffix(name);
        if (text.AsSpan().Equals(nameNoSuffix, StringComparison.OrdinalIgnoreCase))
        {
            Report(ctx, variable);
            return;
        }

        if (EqualsNameWithAlphanumericText(nameNoSuffix, text))
            Report(ctx, variable);
    }

    private static void Report(SymbolAnalysisContext ctx, IVariableSymbol variable) =>
        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.LabelTokenRequiresTokSuffix,
            variable.GetLocation()));

    private static ReadOnlySpan<char> GetNameWithoutApprovedSuffix(string name)
    {
        if (name.Length <= 3)
            return name.AsSpan();

        ReadOnlySpan<char> suffix = name.AsSpan(name.Length - 3, 3);

        for (int i = 0; i < ApprovedSuffixes.Length; i++)
        {
            if (suffix.Equals(ApprovedSuffixes[i].AsSpan(), StringComparison.Ordinal))
                return name.AsSpan(0, name.Length - 3);
        }

        return name.AsSpan();
    }

    private static bool EqualsNameWithAlphanumericText(ReadOnlySpan<char> nameNoSuffix, string text)
    {
        char[] buffer = new char[text.Length];
        int count = 0;

        foreach (char c in text)
        {
            if (char.IsLetterOrDigit(c))
                buffer[count++] = c;
        }

        return nameNoSuffix.Equals(buffer.AsSpan(0, count), StringComparison.OrdinalIgnoreCase);
    }
}