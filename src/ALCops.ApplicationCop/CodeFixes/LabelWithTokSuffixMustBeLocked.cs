using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using ALCops.Common.Reflection;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(LabelWithTokSuffixMustBeLockedFixProvider))]
public sealed class LabelWithTokSuffixMustBeLockedFixProvider : CodeFixProvider
{
    private const string LockedPropertyName = "Locked";

    private class LabelWithTokSuffixMustBeLockedAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public LabelWithTokSuffixMustBeLockedAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.LabelWithTokSuffixMustBeLocked.Id);

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

    private static LabelWithTokSuffixMustBeLockedAction CreateCodeAction(SyntaxNode node, Document document, bool generateFixAll)
    {
        return new LabelWithTokSuffixMustBeLockedAction(
            ApplicationCopAnalyzers.LabelWithTokSuffixMustBeLockedCodeAction,
            ct => SetLockedIsTrueProperty(document, node, ct),
            nameof(LabelWithTokSuffixMustBeLockedFixProvider),
            generateFixAll);
    }

    private static async Task<Document> SetLockedIsTrueProperty(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        LabelSyntax? originalLabel =
            node.FirstAncestorOrSelf<VariableDeclarationSyntax>()?
                .DescendantNodes()
                .OfType<LabelSyntax>()
                .FirstOrDefault();

        if (originalLabel is null)
            return document;

        LabelSyntax newLabel =
            originalLabel.Properties is null
                ? AddLockedPropertyToLabel(originalLabel)
                : SetLockedPropertyToTrue(originalLabel);

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(originalLabel, newLabel);
        return document.WithSyntaxRoot(newRoot);
    }
    private static LabelSyntax AddLockedPropertyToLabel(LabelSyntax label)
    {
        var values = CreateSeparatedList(GetLockedEntryTrue());
        var props = SyntaxFactory.CommaSeparatedIdentifierEqualsLiteralList(values);

        // Ensure the label has a comma before its properties list:  Label 'x', Locked = true
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