using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using ALCops.Common.Reflection;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(PermissionSetCaptionLengthCodeFixProvider))]
public sealed class PermissionSetCaptionLengthCodeFixProvider : CodeFixProvider
{
    private const int MaxCaptionLength = 30;
    private const string MaxLengthName = "MaxLength";

    private class PermissionSetCaptionLengthCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public PermissionSetCaptionLengthCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.PermissionSetCaptionLength.Id);

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

    private static PermissionSetCaptionLengthCodeAction CreateCodeAction(SyntaxNode node, Document document, bool generateFixAll)
    {
        return new PermissionSetCaptionLengthCodeAction(
            ApplicationCopAnalyzers.PermissionSetCaptionLengthCodeAction,
            ct => SetMaxLengthProperty(document, node, ct),
            nameof(PermissionSetCaptionLengthCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> SetMaxLengthProperty(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var originalLabel = node.DescendantNodes().OfType<LabelSyntax>().FirstOrDefault();
        if (originalLabel is null)
            return document;

        LabelSyntax newLabel;
        if (originalLabel.Properties is null)
        {
            newLabel = AddMaxLengthPropertiesToLabel(originalLabel);
        }
        else
        {
            newLabel = UpdateMaxLengthInLabel(originalLabel);
        }

        // Replace the label node (smallest replacement = best formatting stability)
        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(originalLabel, newLabel);
        return document.WithSyntaxRoot(newRoot);
    }

    private static LabelSyntax AddMaxLengthPropertiesToLabel(LabelSyntax label)
    {
        var values = CreateSeparatedList(GetMaxLengthEntry(MaxCaptionLength));
        var props = SyntaxFactory.CommaSeparatedIdentifierEqualsLiteralList(values);

        return label
            .WithCommaToken(SyntaxFactory.Token(EnumProvider.SyntaxKind.CommaToken))
            .WithProperties(props);
    }

    private static LabelSyntax UpdateMaxLengthInLabel(LabelSyntax label)
    {
        var props = label.Properties;
        if (props is null)
            return AddMaxLengthPropertiesToLabel(label);

        var values = props.Values;

        var existing =
            values.FirstOrDefault(v =>
                v.Identifier.ValueText?.Equals(MaxLengthName, StringComparison.OrdinalIgnoreCase) == true);

        var replacement = GetMaxLengthEntry(MaxCaptionLength);

        var newValues =
            existing is null
                ? values.Add(replacement)
                : values.Replace(existing, replacement);

        var newProps = props.WithValues(newValues);
        return label.WithProperties(newProps);
    }

    private static IdentifierEqualsLiteralSyntax GetMaxLengthEntry(int maxLength)
    {
        var literal = SyntaxFactory.Int32SignedLiteralValue(SyntaxFactory.Literal(maxLength));

        return SyntaxFactory.IdentifierEqualsLiteral(
            SyntaxFactory.Identifier(MaxLengthName),
            literal);
    }

    private static SeparatedSyntaxList<IdentifierEqualsLiteralSyntax> CreateSeparatedList(IdentifierEqualsLiteralSyntax single) =>
        default(SeparatedSyntaxList<IdentifierEqualsLiteralSyntax>).Add(single);
}