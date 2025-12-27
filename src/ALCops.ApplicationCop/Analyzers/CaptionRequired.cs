using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public class CaptionRequired : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.CaptionRequired);

    private static readonly HashSet<string> _predefinedActionCategoryNames =
        SyntaxFacts.PredefinedActionCategoryNames.Select(x => x.Key.ToLowerInvariant()).ToHashSet();

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            CheckForMissingCaptions,
            EnumProvider.SymbolKind.Page,
            EnumProvider.SymbolKind.Query,
            EnumProvider.SymbolKind.Table,
            EnumProvider.SymbolKind.Field,
            EnumProvider.SymbolKind.Action,
            EnumProvider.SymbolKind.EnumValue,
            EnumProvider.SymbolKind.Control,
            EnumProvider.SymbolKind.PermissionSet
        );

    private void CheckForMissingCaptions(SymbolAnalysisContext context)
    {
        if (context.IsObsolete())
            return;

        if (context.Symbol.Kind == EnumProvider.SymbolKind.Control)
        {
            var Control = (IControlSymbol)context.Symbol;
            switch (Control.ControlKind)
            {
                case var _ when Control.ControlKind == EnumProvider.ControlKind.Field:
                    if (CaptionIsMissing(context.Symbol, context))
                        if (Control.RelatedFieldSymbol is not null)
                        {
                            if (CaptionIsMissing(Control.RelatedFieldSymbol, context))
                                RaiseDiagnostic(context);
                        }
                        else
                        {
                            if (!SuppressCaptionWarning(context))
                                RaiseDiagnostic(context);
                        }
                    break;

                case var _ when Control.ControlKind == EnumProvider.ControlKind.Area:
                    break;

                case var _ when Control.ControlKind == EnumProvider.ControlKind.Grid:
                    break;

                case var _ when Control.ControlKind == EnumProvider.ControlKind.Repeater:
                    break;

                case var _ when Control.ControlKind == EnumProvider.ControlKind.Part:
                    if (CaptionIsMissing(context.Symbol, context))
                        if (Control.RelatedPartSymbol is not null)
                            if (CaptionIsMissing(Control.RelatedPartSymbol, context))
                                if (!SuppressCaptionWarning(context))
                                    RaiseDiagnostic(context);
                    break;

                case var _ when Control.ControlKind == EnumProvider.ControlKind.UserControl:
                    break;

                case var _ when Control.ControlKind == EnumProvider.ControlKind.SystemPart:
                    break;

                default:
                    if (CaptionIsMissing(context.Symbol, context))
                        RaiseDiagnostic(context);
                    break;
            }
        }
        else if (context.Symbol is IActionSymbol actionSymbol)
        {
            switch (actionSymbol.ActionKind)
            {
                case var _ when actionSymbol.ActionKind == EnumProvider.ActionKind.Action:
                    if (CaptionIsMissing(context.Symbol, context))
                        RaiseDiagnostic(context);
                    break;

                case var _ when actionSymbol.ActionKind == EnumProvider.ActionKind.Group:
                    if (context.Symbol.GetEnumPropertyValue<ShowAsKind>(EnumProvider.PropertyKind.ShowAs) == EnumProvider.ShowAsKind.SplitButton)
                    {
                        // There is one specifc case where a Caption is needed on a Group where the property ShowAs is set to SplitButton
                        // A) The group is inside a Promoted Area
                        // B) Has one or more actionrefs
                        // C) One of the actions of the actionsrefs has Scope set to Repeater

                        if (context.Symbol.ContainingSymbol is not IActionSymbol containingSymbol)
                            return;

                        if (containingSymbol.ActionKind != EnumProvider.ActionKind.Area)
                            break;

                        if (!SemanticFacts.IsSameName(context.Symbol.ContainingSymbol.Name, "Promoted"))
                            break;

                        if (!actionSymbol.Actions.Where(a => a.ActionKind == EnumProvider.ActionKind.ActionRef)
                                                 .Where(a => a.Target?.GetEnumPropertyValueOrDefault<PageActionScopeKind>(EnumProvider.PropertyKind.Scope) == EnumProvider.PageActionScopeKind.Repeater)
                                                 .Any())
                            break;

                        if (CaptionIsMissing(context.Symbol, context))
                            RaiseDiagnostic(context);
                        break;
                    }
                    else
                    {
                        if (CaptionIsMissing(context.Symbol, context))
                            RaiseDiagnostic(context);
                        break;
                    }
            }
        }
        else if (context.Symbol.Kind == EnumProvider.SymbolKind.EnumValue)
        {
            IEnumValueSymbol enumValueSymbol = (IEnumValueSymbol)context.Symbol;
            if (enumValueSymbol.Name != "" && CaptionIsMissing(context.Symbol, context))
                RaiseDiagnostic(context);
        }
        else if (context.Symbol.Kind == EnumProvider.SymbolKind.Page)
        {
            if (((IPageTypeSymbol)context.Symbol).PageType != PageTypeKind.API)
                if (CaptionIsMissing(context.Symbol, context))
                    RaiseDiagnostic(context);
        }
        else if (context.Symbol.Kind == EnumProvider.SymbolKind.PermissionSet)
        {
            IPropertySymbol? assignableProperty = context.Symbol.GetProperty(EnumProvider.PropertyKind.Assignable);
            if (assignableProperty is null || (bool)assignableProperty.Value)
                if (CaptionIsMissing(context.Symbol, context))
                    RaiseDiagnostic(context);
        }
        else
        {
            if (CaptionIsMissing(context.Symbol, context))
                RaiseDiagnostic(context);
        }
    }

    private bool CaptionIsMissing(ISymbol Symbol, SymbolAnalysisContext context)
    {
        if (Symbol.ContainingType?.Kind == EnumProvider.SymbolKind.Table)
        {
            if (((ITableTypeSymbol)Symbol.ContainingType).Id >= 2000000000)
                return false;

            if (((IFieldSymbol)Symbol).Id >= 2000000000)
                return false;
        }

        if (Symbol.Kind == EnumProvider.SymbolKind.Action && ((IActionSymbol)Symbol).ActionKind == EnumProvider.ActionKind.Group && _predefinedActionCategoryNames.Contains(Symbol.Name.ToLowerInvariant()))
            return false;

        if (Symbol.GetBooleanPropertyValue(EnumProvider.PropertyKind.ShowCaption) != false)
            if (Symbol.GetProperty(EnumProvider.PropertyKind.Caption) is null && Symbol.GetProperty(EnumProvider.PropertyKind.CaptionClass) is null && Symbol.GetProperty(EnumProvider.PropertyKind.CaptionML) is null)
                return true;
        return false;
    }

    private static bool SuppressCaptionWarning(SymbolAnalysisContext context)
    {
        if (context.Symbol.GetContainingObjectTypeSymbol().GetTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Page)
            return false;

        IPageTypeSymbol pageTypeSymbol = (IPageTypeSymbol)context.Symbol.GetContainingObjectTypeSymbol();
        if (pageTypeSymbol.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Page || pageTypeSymbol.PageType != EnumProvider.PageTypeKind.API)
            return false;

        return true;
    }

    private void RaiseDiagnostic(SymbolAnalysisContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.CaptionRequired,
            context.Symbol.GetLocation()));
    }
}