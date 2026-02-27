using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.InternalSyntax;

namespace ALCops.TestAutomationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class GlobalMethodRequiresTestAttribute : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.GlobalMethodRequiresTestAttribute
        );

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSymbolAction(
            this.AnalyzeMethod,
            EnumProvider.SymbolKind.Method);
    }

    private void AnalyzeMethod(SymbolAnalysisContext ctx)
    {
        if (ctx.Symbol is not IMethodSymbol method || method.IsLocal)
            return;

        if (!IsTestCodeunit(ctx.Symbol.GetContainingSymbolOfKind<IObjectTypeSymbol>(EnumProvider.SymbolKind.Codeunit)))
            return;

        if (IsTestMethod(method) || IsHandlerMethod(method))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.GlobalMethodRequiresTestAttribute,
            ctx.Symbol.GetLocation(),
            method.Name
        ));
    }

    private static bool IsTestCodeunit(IObjectTypeSymbol? symbol)
    {
        if (symbol == null)
        {
            return false;
        }

        var property = symbol.GetEnumPropertyValue<CodeunitSubtypeKind>(EnumProvider.PropertyKind.Subtype);
        return property != null && property == EnumProvider.CodeunitSubtypeKind.Test;
    }

    private static bool IsTestMethod(IMethodSymbol method) =>
        method.Attributes.Any(attr => attr.AttributeKind == EnumProvider.AttributeKind.Test);

    private static bool IsHandlerMethod(IMethodSymbol method) =>
        method.Attributes.Any(attr => IsHandlerAttribute(attr.AttributeKind));

    private static bool IsHandlerAttribute(AttributeKind kind)
    {
        return kind == EnumProvider.AttributeKind.ConfirmHandler
            || kind == EnumProvider.AttributeKind.FilterPageHandler
            || kind == EnumProvider.AttributeKind.HttpClientHandler
            || kind == EnumProvider.AttributeKind.HyperlinkHandler
            || kind == EnumProvider.AttributeKind.MessageHandler
            || kind == EnumProvider.AttributeKind.ModalPageHandler
            || kind == EnumProvider.AttributeKind.PageHandler
            || kind == EnumProvider.AttributeKind.RecallNotificationHandler
            || kind == EnumProvider.AttributeKind.ReportHandler
            || kind == EnumProvider.AttributeKind.RequestPageHandler
            || kind == EnumProvider.AttributeKind.SendNotificationHandler
            || kind == EnumProvider.AttributeKind.SessionSettingsHandler
            || kind == EnumProvider.AttributeKind.StrMenuHandler;
    }
}