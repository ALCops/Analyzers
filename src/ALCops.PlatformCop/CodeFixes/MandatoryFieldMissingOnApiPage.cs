using System.Collections.Immutable;
using ALCops.Common.Reflection;
using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(MandatoryFieldMissingOnApiPageCodeFix))]
public sealed class MandatoryFieldMissingOnApiPageCodeFix : CodeFixProvider
{
    private static readonly ImmutableArray<(string RecField, string ControlName)> MandatoryFields =
        ImmutableArray.Create(
            (RecField: "SystemId", ControlName: "id"),
            (RecField: "SystemModifiedAt", ControlName: "lastModifiedDateTime"));

    private class MandatoryFieldMissingOnApiPageCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;


        public MandatoryFieldMissingOnApiPageCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.MandatoryFieldMissingOnApiPage.Id);

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
        SyntaxNode node = syntaxRoot.FindNode(span).FirstAncestorOrSelf<PageSyntax>();
        ctx.RegisterCodeFix(CreateCodeAction(node, document, generateFixAll: true), ctx.Diagnostics[0]);
    }

    private static MandatoryFieldMissingOnApiPageCodeAction CreateCodeAction(SyntaxNode node, Document document, bool generateFixAll)
    {
        return new MandatoryFieldMissingOnApiPageCodeAction(
            PlatformCopAnalyzers.MandatoryFieldMissingOnApiPageCodeAction,
            ct => AddMissingFields(document, node, ct),
            nameof(MandatoryFieldMissingOnApiPageCodeFix),
            generateFixAll);
    }

    private static async Task<Document> AddMissingFields(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var layout = node.DescendantNodes().OfType<PageLayoutSyntax>().FirstOrDefault();
        if (layout is null)
            return document;

        var repeaters = layout
            .DescendantNodes()
            .OfType<PageAreaSyntax>()
            .Select(a =>
                a.DescendantNodes()
                 .OfType<PageGroupSyntax>()
                 .FirstOrDefault(g => g.ControlKeyword.ValueText.IsSameName("repeater")))
            .Where(r => r is not null)
            .ToArray();

        if (repeaters.Length != 1)
            return document;

        var repeaterGroup = repeaters[0];
        if (repeaterGroup is null)
            return document;

        var existingFields = repeaterGroup.Controls.OfType<PageFieldSyntax>().ToArray();
        var missing = new List<(string RecField, string ControlName)>(capacity: MandatoryFields.Length);
        foreach (var mandatoryField in MandatoryFields)
        {
            bool exists = existingFields.Any(f =>
                f.Name.Identifier.ValueText.IsSameName(mandatoryField.ControlName) &&
                SemanticFacts.IsSameName(f.Expression.ToString(), $"Rec.{mandatoryField.RecField}"));

            if (!exists)
                missing.Add(mandatoryField);
        }

        if (missing.Count == 0)
            return document;

        var referenceField = existingFields.FirstOrDefault();
        var eolTrivia =
            referenceField?
                .GetLastToken()
                .TrailingTrivia
                .FirstOrDefault(t => t.IsKind(EnumProvider.SyntaxKind.EndOfLineTrivia));

        var newControls = new List<ControlBaseSyntax>(missing.Count);
        foreach (var (RecField, ControlName) in missing)
        {
            var newField = CreatePageField(ControlName, RecField, eolTrivia);

            if (referenceField is not null)
                newField = newField.WithLeadingTrivia(referenceField.GetLeadingTrivia());

            newControls.Add(newField);
        }

        var updatedRepeater = repeaterGroup.WithControls(repeaterGroup.Controls.AddRange(newControls));

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(repeaterGroup, updatedRepeater);
        return document.WithSyntaxRoot(newRoot);
    }

    private static PageFieldSyntax CreatePageField(string controlName, string fieldName, SyntaxTrivia? endOfLineTrivia)
    {
        var empty = SyntaxFactory.TriviaList();
        var space = SyntaxFactory.TriviaList(SyntaxFactory.Space);
        var eol = endOfLineTrivia is null
            ? SyntaxFactory.TriviaList()
            : SyntaxFactory.TriviaList(endOfLineTrivia.Value);

        return SyntaxFactory.PageField(
            SyntaxFactory.Token(EnumProvider.SyntaxKind.FieldKeyword),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.OpenParenToken),
            SyntaxFactory.IdentifierName(controlName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.SemicolonToken),
            CreateRecFieldExpression(fieldName),
            SyntaxFactory.Token(empty, EnumProvider.SyntaxKind.CloseParenToken, space),
            SyntaxFactory.Token(empty, EnumProvider.SyntaxKind.OpenBraceToken, space),
            SyntaxFactory.PropertyList(),
            default,
            SyntaxFactory.Token(empty, EnumProvider.SyntaxKind.CloseBraceToken, eol));
    }

    private static QualifiedNameSyntax CreateRecFieldExpression(string fieldName)
    {
        // Builds: Rec.<FieldName>
        return SyntaxFactory.QualifiedName(
            SyntaxFactory.IdentifierName("Rec"),
            SyntaxFactory.IdentifierName(fieldName));
    }
}