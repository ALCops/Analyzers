using System.Collections.Immutable;
using ALCops.Common.Reflection;
using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class EventSubscriberVarKeyword : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.EventSubscriberVarKeyword
        );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            CheckForEventSubscriberVar,
            EnumProvider.SymbolKind.Method);

    private void CheckForEventSubscriberVar(SymbolAnalysisContext ctx)
    {
        if (ctx.Symbol is not IMethodSymbol subscriberMethod)
            return;

        var eventSubscriberAttribute = subscriberMethod.Attributes
            .FirstOrDefault(attr => attr.AttributeKind == EnumProvider.AttributeKind.EventSubscriber);

        if (eventSubscriberAttribute is null)
            return;

        if (eventSubscriberAttribute.Arguments.Length < 3)
            return;

        IMethodSymbol? publisherMethod = eventSubscriberAttribute.Arguments[2].ValueAsSymbol as IMethodSymbol
            // Fallback for when the event name is declared as a string literal
            ?? GetValueAsSymbolFromStringLiteral(ctx, eventSubscriberAttribute);

        if (publisherMethod is null)
            return;

        var publisherParameters = publisherMethod.Parameters;

        foreach (var subscriberParameter in subscriberMethod.Parameters)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            var publisherParameter = publisherParameters
                .FirstOrDefault(p =>
                    string.Equals(p.Name, subscriberParameter.Name, StringComparison.OrdinalIgnoreCase));

            if (publisherParameter is null)
                continue;

            if (publisherParameter.IsVar && !subscriberParameter.IsVar)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.EventSubscriberVarKeyword,
                    subscriberParameter.GetLocation(),
                    subscriberParameter.Name));
            }
        }
    }

    private static IMethodSymbol? GetValueAsSymbolFromStringLiteral(SymbolAnalysisContext context, IAttributeSymbol eventSubscriberAttribute)
    {
        var eventNameArgument = eventSubscriberAttribute.Arguments[2];
        if (eventNameArgument.Type.NavTypeKind != EnumProvider.NavTypeKind.String)
            return null;

        var eventName = eventSubscriberAttribute.Arguments[2].ValueText;
        if (string.IsNullOrEmpty(eventName))
            return null;

        var applicationObject = eventSubscriberAttribute.GetReferencedApplicationObject();
        if (applicationObject is null)
            return null;

        return applicationObject.FindMethodByNameAcrossModules(eventName, context.Compilation);
    }
}