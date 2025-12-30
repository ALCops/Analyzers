using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using ALCops.Common.Reflection;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(EmptyCaptionLockedCodeFixProvider))]
public sealed class EmptyCaptionLockedCodeFixProvider : CodeFixProvider
{
    private const string CaptionPropertyName = "Caption";
    private const string LockedPropertyName = "Locked";

    private class EmptyCaptionLockedCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public EmptyCaptionLockedCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.EmptyCaptionLocked.Id);

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
        SyntaxNode node = syntaxRoot.FindNode(span);
        ctx.RegisterCodeFix(CreateCodeAction(node, document, generateFixAll: true), ctx.Diagnostics[0]);
    }

    private static EmptyCaptionLockedCodeAction CreateCodeAction(SyntaxNode node, Document document, bool generateFixAll)
    {
        return new EmptyCaptionLockedCodeAction(
            ApplicationCopAnalyzers.EmptyCaptionLockedCodeAction,
            ct => SetLockedIsTrueProperty(document, node, ct),
            nameof(EmptyCaptionLockedCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> SetLockedIsTrueProperty(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var captionPropertyValue =
                node.Parent.DescendantNodesAndSelf()
                           .OfType<LabelPropertyValueSyntax>()
                           .FirstOrDefault();

        if (captionPropertyValue is null)
            return document;

        var originalCaptionValue = captionPropertyValue.Value;
        if (originalCaptionValue is null)
            return document;

        LabelSyntax newCaptionValue =
            originalCaptionValue.Properties is null
                ? AddLockedPropertyToLabel(originalCaptionValue)
                : SetLockedPropertyToTrue(originalCaptionValue);

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(originalCaptionValue, newCaptionValue);
        return document.WithSyntaxRoot(newRoot);
    }

    private static LabelSyntax AddLockedPropertyToLabel(LabelSyntax label)
    {
        var values = CreateSeparatedList(GetLockedEntryTrue());
        var props = SyntaxFactory.CommaSeparatedIdentifierEqualsLiteralList(values);

        return label
            .WithCommaToken(SyntaxFactory.Token(EnumProvider.SyntaxKind.CommaToken))
            .WithProperties(props);
    }

    private static LabelSyntax SetLockedPropertyToTrue(LabelSyntax label)
    {
        var props = label.Properties;
        if (props is null)
            return AddLockedPropertyToLabel(label);

        var values = props.Values;

        var existing =
            values.FirstOrDefault(v =>
                v.Identifier.ValueText?.Equals(LockedPropertyName, StringComparison.Ordinal) == true);

        var replacement = GetLockedEntryTrue();

        var newValues =
            existing is null
                ? values.Add(replacement)
                : values.Replace(existing, replacement);

        var newProps = props.WithValues(newValues);
        return label.WithProperties(newProps);
    }

    private static IdentifierEqualsLiteralSyntax GetLockedEntryTrue()
    {
        var literal = SyntaxFactory.BooleanLiteralValue(
            SyntaxFactory.Token(EnumProvider.SyntaxKind.TrueKeyword));

        return SyntaxFactory.IdentifierEqualsLiteral(
            SyntaxFactory.Identifier(LockedPropertyName),
            literal);
    }

    private static SeparatedSyntaxList<IdentifierEqualsLiteralSyntax> CreateSeparatedList(IdentifierEqualsLiteralSyntax single) =>
        default(SeparatedSyntaxList<IdentifierEqualsLiteralSyntax>).Add(single);
}