using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class IdentifiersInEventSubscribers : DiagnosticAnalyzer
{
    private const string EventSubscriberAttributeName = "EventSubscriber";
    private const int EventNameArgIndex = 2;
    private const int ElementNameArgIndex = 3;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.IdentifiersInEventSubscribers);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCodeBlockAction(
            this.AnalyzeIdentifiersInEventSubscribers);

    private void AnalyzeIdentifiersInEventSubscribers(CodeBlockAnalysisContext ctx)
    {
        if (!VersionChecker.IsSupported(ctx.OwningSymbol, EnumProvider.Feature.IdentifiersInEventSubscribers))
            return;

        if (ctx.CodeBlock is not MethodDeclarationSyntax method)
            return;

        var attributes = method.Attributes;
        if (attributes.Count == 0)
            return;

        var eventSubscriberAttribute =
            attributes.FirstOrDefault(attr =>
            {
                var nameText = attr.GetIdentifierOrLiteralValue();
                return nameText is not null &&
                       SemanticFacts.IsSameName(nameText, EventSubscriberAttributeName);
            });
        if (eventSubscriberAttribute is null)
            return;

        var argList = eventSubscriberAttribute.ArgumentList;
        if (argList is null)
            return;

        var args = argList.Arguments;

        // Check EventName (index 2): must be identifier syntax, not a string literal
        if (args.Count > EventNameArgIndex)
        {
            var eventNameArg = args[EventNameArgIndex];
            if (eventNameArg.IsKind(EnumProvider.SyntaxKind.LiteralAttributeArgument))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.IdentifiersInEventSubscribers,
                    eventNameArg.GetLocation()));

                // Single diagnostic per method; EventName takes precedence
                return;
            }
        }

        // Check ElementName (index 3): only if present and non-empty, and must not be a string literal
        if (args.Count > ElementNameArgIndex)
        {
            var elementNameArg = args[ElementNameArgIndex];
            if (elementNameArg.IsKind(EnumProvider.SyntaxKind.LiteralAttributeArgument) &&
                !string.IsNullOrEmpty(elementNameArg.GetIdentifierOrLiteralValue()))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.IdentifiersInEventSubscribers,
                    elementNameArg.GetLocation()));
            }
        }
    }
}