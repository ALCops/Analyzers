using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class DuplicatePropertyBetweenActionAndRunObject : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.DuplicatePropertyBetweenActionAndRunObject);

    public override VersionCompatibility SupportedVersions =>
        VersionProvider.VersionCompatibility.Fall2026OrGreater;

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            Analyze,
            EnumProvider.SymbolKind.Action,
            EnumProvider.SymbolKind.Change);

    private static void Analyze(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Symbol is IActionSymbol action)
        {
            var target = GetRunObjectTarget(action);
            if (target is not null)
                CompareProperties(ctx, action, target, baseAction: null);

            return;
        }

        if (ctx.Symbol.Kind == EnumProvider.SymbolKind.Change &&
            ctx.Symbol is IChangeSymbol change &&
            change.ChangeKind == EnumProvider.ChangeKind.Modify)
        {
            AnalyzeActionModification(ctx, change);
        }
    }

    // A modify block cannot set RunObject (the property is not extension-modifiable), so the modified
    // base action's target is the one the runtime falls back to.
    private static void AnalyzeActionModification(SymbolAnalysisContext ctx, IChangeSymbol change)
    {
        if (change.GetContainingApplicationObjectTypeSymbol() is not IPageExtensionBaseTypeSymbol extension ||
            extension.Kind != EnumProvider.SymbolKind.PageExtension ||
            extension.Target is not IPageBaseTypeSymbol basePage)
        {
            return;
        }

        foreach (var baseAction in basePage.FlattenedActions)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            if (!SemanticFacts.IsSameName(baseAction.Name, change.Name))
                continue;

            var target = GetRunObjectTarget(baseAction);
            if (target is not null && !baseAction.IsObsolete())
                CompareProperties(ctx, change, target, baseAction);

            return;
        }
    }

    private static IApplicationObjectTypeSymbol? GetRunObjectTarget(IActionSymbol action)
    {
        if (action.ActionKind != EnumProvider.ActionKind.Action)
            return null;

        // Source actions bind RunObject to the target object symbol; actions read from a referenced
        // app store the reference as a string, which this cast skips.
        var target = action.GetProperty(EnumProvider.PropertyKind.RunObject)?.Value as IApplicationObjectTypeSymbol;
        if (target is not (IPageTypeSymbol or IReportTypeSymbol) || target.IsObsolete())
            return null;

        return target;
    }

    private static void CompareProperties(
        SymbolAnalysisContext ctx,
        ISymbol actionOrChange,
        IApplicationObjectTypeSymbol target,
        IActionSymbol? baseAction)
    {
        // A report's AboutTitle and AboutText live on its request page. A report read from a referenced
        // app does not list its request page among its members, so its About* values are not compared.
        ISymbol? aboutOwner = target is IReportTypeSymbol
            ? target.GetMembers().OfType<IPageBaseTypeSymbol>().FirstOrDefault()
            : target;

        CompareProperty(ctx, actionOrChange, target, baseAction, target, EnumProvider.PropertyKind.Caption, EnumProvider.PropertyKind.CaptionML);
        CompareProperty(ctx, actionOrChange, target, baseAction, target, EnumProvider.PropertyKind.ToolTip, EnumProvider.PropertyKind.ToolTipML);
        CompareProperty(ctx, actionOrChange, target, baseAction, aboutOwner, EnumProvider.PropertyKind.AboutTitle, EnumProvider.PropertyKind.AboutTitleML);
        CompareProperty(ctx, actionOrChange, target, baseAction, aboutOwner, EnumProvider.PropertyKind.AboutText, EnumProvider.PropertyKind.AboutTextML);
    }

    private static void CompareProperty(
        SymbolAnalysisContext ctx,
        ISymbol actionOrChange,
        IApplicationObjectTypeSymbol target,
        IActionSymbol? baseAction,
        ISymbol? valueOwner,
        PropertyKind kind,
        PropertyKind multilanguageKind)
    {
        if (valueOwner is null)
            return;

        var actionProperty = actionOrChange.GetProperty(kind);
        // An empty value (ToolTip = '') deliberately suppresses the inherited one; it is not a copy.
        if (actionProperty is null || string.IsNullOrEmpty(actionProperty.ValueText))
            return;

        // A modify block that overrides a value the base action sets itself replaces that value, not the
        // one inherited from the target, so removing it would change the caption.
        if (baseAction is not null &&
            (baseAction.GetProperty(kind) is not null || baseAction.GetProperty(multilanguageKind) is not null))
        {
            return;
        }

        var targetProperty = valueOwner.GetProperty(kind);
        if (targetProperty is null)
            return;

        // Ordinal on purpose: a value that differs only in case is a different translation unit.
        if (!string.Equals(actionProperty.ValueText, targetProperty.ValueText, StringComparison.Ordinal))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DuplicatePropertyBetweenActionAndRunObject,
            actionProperty.GetLocation(),
            kind.ToString(),
            actionOrChange.Name,
            target is IPageTypeSymbol ? "page" : "report",
            target.Name));
    }
}
