using System.Collections.Immutable;
using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.FormattingCop.CodeFixes;

[CodeFixProvider(nameof(CasingMismatchCodeFix))]
public sealed class CasingMismatchCodeFix : CodeFixProvider
{
#if NETSTANDARD2_1
    // C# 9 records require 'System.Runtime.CompilerServices.IsExternalInit' which doesn't exist in netstandard2.1.
    // We use a regular class for netstandard2.1 and a record for .NET 8+ to maintain compatibility with both targets.
    private sealed class CodeFixProperties
    {
        public string CanonicalText { get; }

        private CodeFixProperties(string canonicalText)
        {
            CanonicalText = canonicalText;
        }

        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(CanonicalText), out var canonicalText) || string.IsNullOrEmpty(canonicalText))
                return null;

            return new CodeFixProperties(canonicalText);
        }
    }
#endif

#if NET8_0_OR_GREATER
    private sealed record CodeFixProperties(string CanonicalText)
    {
        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(CanonicalText), out var canonicalText) || string.IsNullOrEmpty(canonicalText))
                return null;

            return new CodeFixProperties(canonicalText);
        }
    }
#endif

    private class CasingMismatchCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public CasingMismatchCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.CasingMismatch.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
         WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        Document document = ctx.Document;
        TextSpan span = ctx.Span;
        CancellationToken cancellationToken = ctx.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        RegisterInstanceCodeFix(ctx, syntaxRoot, span, document);
    }

    private static void RegisterInstanceCodeFix(CodeFixContext ctx, SyntaxNode syntaxRoot, TextSpan span, Document document)
    {
        var diagnostic = ctx.Diagnostics
                  .FirstOrDefault(d => d.Id == DiagnosticDescriptors.CasingMismatch.Id);

        var properties = CodeFixProperties.TryParse(diagnostic?.Properties);
        if (properties is null)
            return;

        ctx.RegisterCodeFix(CreateCodeAction(span, document, properties, true), ctx.Diagnostics[0]);
    }

    private static CasingMismatchCodeAction CreateCodeAction(TextSpan span, Document document, CodeFixProperties properties, bool generateFixAll)
    {
        return new CasingMismatchCodeAction(
            FormattingCopAnalyzers.CasingMismatchCodeAction,
            ct => ReplaceSourceSpanText(document, span, properties, ct),
            nameof(CasingMismatchCodeFix),
            generateFixAll);
    }

    private static async Task<Document> ReplaceSourceSpanText(Document document, TextSpan span, CodeFixProperties properties, CancellationToken cancellationToken)
    {
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var NewSourceText = sourceText.WithChanges(new TextChange(span, properties.CanonicalText.QuoteIdentifierIfNeededWithReflection()));
        return document.WithText(NewSourceText);
    }
}