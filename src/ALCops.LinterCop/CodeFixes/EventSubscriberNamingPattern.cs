using System.Collections.Immutable;
using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.LinterCop.CodeFixes;

[CodeFixProvider(nameof(EventSubscriberNamingPatternCodeFixProvider))]
public sealed class EventSubscriberNamingPatternCodeFixProvider : CodeFixProvider
{
#if NETSTANDARD2_1
    // C# 9 records require 'System.Runtime.CompilerServices.IsExternalInit' which doesn't exist in netstandard2.1.
    // We use a regular class for netstandard2.1 and a record for .NET 8+ to maintain compatibility with both targets.
    private sealed class CodeFixProperties
    {
        public string PreferredName { get; }

        private CodeFixProperties(string preferredName)
        {
            PreferredName = preferredName;
        }

        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(PreferredName), out var preferredName) || string.IsNullOrEmpty(preferredName))
                return null;

            return new CodeFixProperties(preferredName);
        }
    }
#endif

#if NET8_0_OR_GREATER
    private sealed record CodeFixProperties(string PreferredName)
    {
        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(PreferredName), out var preferredName) || string.IsNullOrEmpty(preferredName))
                return null;

            return new CodeFixProperties(preferredName);
        }
    }
#endif

    private sealed class EventSubscriberNamingPatternCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public EventSubscriberNamingPatternCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.EventSubscriberNamingPattern.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
         WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        Document document = ctx.Document;
        TextSpan span = ctx.Span;
        CancellationToken cancellationToken = ctx.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);
        RegisterInstanceCodeFix(ctx, span, document);
    }

    private static void RegisterInstanceCodeFix(CodeFixContext ctx, TextSpan span, Document document)
    {
        var diagnostic = ctx.Diagnostics[0];
        var properties = CodeFixProperties.TryParse(diagnostic.Properties);

        if (properties is null)
            return;

        ctx.RegisterCodeFix(
            CreateCodeAction(span, document, properties, generateFixAll: true),
            diagnostic);
    }

    private static EventSubscriberNamingPatternCodeAction CreateCodeAction(TextSpan span, Document document,
        CodeFixProperties properties, bool generateFixAll)
    {
        return new EventSubscriberNamingPatternCodeAction(
            LinterCopAnalyzers.EventSubscriberNamingPatternCodeAction,
            ct => RenameSubscriber(document, span, properties, ct),
            nameof(EventSubscriberNamingPatternCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> RenameSubscriber(Document document, TextSpan span,
        CodeFixProperties properties, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var oldToken = root.FindToken(span.Start);

        var newToken = SyntaxFactory.Identifier(properties.PreferredName.QuoteIdentifierIfNeededWithReflection())
            .WithTriviaFrom(oldToken);

        var newRoot = root.ReplaceToken(oldToken, newToken);

        return document.WithSyntaxRoot(newRoot);
    }
}
