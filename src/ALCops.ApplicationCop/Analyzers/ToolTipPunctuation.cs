using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ToolTipPunctuation : DiagnosticAnalyzer
{
    private static readonly List<Punctuation> DefaultAllowedPunctuations =
    [
        new Punctuation { Character = ".", Name = "dot" }
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.ToolTipDoNotUseLineBreaks,
            DiagnosticDescriptors.ToolTipMaximumLength,
            DiagnosticDescriptors.ToolTipMustEndWithPunctuation,
            DiagnosticDescriptors.ToolTipShouldStartWithSpecifies
        );

    // https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/user-assistance#guidelines-for-tooltip-text
    // Try to not exceed 200 characters including spaces.
    // Including the double quote at the beginning and end of the string, makes this a total of 202
    private const int MaxTooltipLength = 202;

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            AnalyzeToolTipPunctuation,
            EnumProvider.SyntaxKind.PageField,
            EnumProvider.SyntaxKind.PageAction,
            EnumProvider.SyntaxKind.Field,
            EnumProvider.SyntaxKind.PageAnalysisView
        );

    private void AnalyzeToolTipPunctuation(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        var tooltipProperty = ctx.Node.GetPropertyValue(EnumProvider.PropertyKind.ToolTip);
        if (tooltipProperty is null)
            return;

        if (tooltipProperty is not LabelPropertyValueSyntax labelProperty)
            return;

        string tooltipText = labelProperty.Value.LabelText.Value.ToString();

        if (ctx.IsDiagnosticEnabled(DiagnosticDescriptors.ToolTipDoNotUseLineBreaks))
            AnalyzeLineBreaks(ctx, tooltipText, tooltipProperty);

        if (ctx.IsDiagnosticEnabled(DiagnosticDescriptors.ToolTipMaximumLength))
            AnalyzeMaximumLength(ctx, tooltipText, tooltipProperty);

        if (ctx.IsDiagnosticEnabled(DiagnosticDescriptors.ToolTipMustEndWithPunctuation))
            AnalyzeEndsWithPunctuation(ctx, tooltipText, tooltipProperty);

        if (ctx.IsDiagnosticEnabled(DiagnosticDescriptors.ToolTipShouldStartWithSpecifies))
            AnalyzeStartsWithSpecifies(ctx, tooltipText, tooltipProperty);
    }

    private static void AnalyzeLineBreaks(SyntaxNodeAnalysisContext ctx, string tooltipText, PropertyValueSyntax tooltipProperty)
    {
        if (tooltipText.Contains('\\'))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ToolTipDoNotUseLineBreaks,
                tooltipProperty.GetLocation()));
        }
    }

    private static void AnalyzeMaximumLength(SyntaxNodeAnalysisContext ctx, string tooltipText, PropertyValueSyntax tooltipProperty)
    {
        if (tooltipText.Length > MaxTooltipLength)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ToolTipMaximumLength,
                tooltipProperty.GetLocation()));
        }
    }

    private static void AnalyzeEndsWithPunctuation(SyntaxNodeAnalysisContext ctx, string tooltipText, PropertyValueSyntax tooltipProperty)
    {
        var fileSystem = ctx.SemanticModel.Compilation.FileSystem;
        var settings = ALCopsSettingsProvider.GetSettings(fileSystem);

        var allowedPunctuations = ResolveAllowedPunctuations(settings);

        foreach (Punctuation punctuation in allowedPunctuations)
        {
            if (tooltipText.EndsWith(punctuation.Character + "'", StringComparison.Ordinal))
            {
                return;
            }
        }

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ToolTipMustEndWithPunctuation,
            tooltipProperty.GetLocation(),
            string.Join("', '", allowedPunctuations.Select(p => p.Name))));
    }

    private static List<Punctuation> ResolveAllowedPunctuations(ALCopsSettings settings)
    {
        if (settings.ToolTipAllowedPunctuations is null or { Count: 0 })
        {
            return DefaultAllowedPunctuations;
        }

        var normalizedPunctuations = settings.ToolTipAllowedPunctuations
            .Where(p => !string.IsNullOrWhiteSpace(p.Character))
            .Select(p => new Punctuation
            {
                Character = p.Character,
                Name = string.IsNullOrWhiteSpace(p.Name) ? p.Character : p.Name
            })
            .ToList();

        return normalizedPunctuations.Count == 0 ? DefaultAllowedPunctuations : normalizedPunctuations;
    }

    private static void AnalyzeStartsWithSpecifies(SyntaxNodeAnalysisContext ctx, string tooltipText, PropertyValueSyntax tooltipProperty)
    {
        if (!MustStartWithSpecifies(ctx))
            return;

        if (!tooltipText.StartsWith("'Specifies", StringComparison.Ordinal))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ToolTipShouldStartWithSpecifies,
                tooltipProperty.GetLocation()));
        }
    }

    private static bool MustStartWithSpecifies(SyntaxNodeAnalysisContext ctx)
    {
        // Table fields 
        if (ctx.ContainingSymbol.Kind == EnumProvider.SymbolKind.Field)
            return true;

        // Page field controls
        if (ctx.ContainingSymbol.Kind == EnumProvider.SymbolKind.Control &&
            ctx.ContainingSymbol is IControlSymbol control &&
            control.ControlKind == EnumProvider.ControlKind.Field)
        {
            return true;
        }

        return false;
    }
}