using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(InstallAndUpgradeCodeunitsShouldBeInternalCodeFixProvider))]
public sealed class InstallAndUpgradeCodeunitsShouldBeInternalCodeFixProvider : CodeFixProvider
{
    private const string AccessPropertyName = "Access";
    private const string InternalAccessValue = "Internal";

    private class InstallAndUpgradeCodeunitsShouldBeInternalCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public InstallAndUpgradeCodeunitsShouldBeInternalCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.InstallAndUpgradeCodeunitsShouldBeInternal.Id);

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
        var token = syntaxRoot.FindToken(span.Start);

        var codeunitSyntax =
                   token.Parent?.FirstAncestorOrSelf<CodeunitSyntax>() ??
                   syntaxRoot.FindNode(span).FirstAncestorOrSelf<CodeunitSyntax>();

        if (codeunitSyntax is null)
            return;

        ctx.RegisterCodeFix(CreateCodeAction(codeunitSyntax, document, true), ctx.Diagnostics[0]);
    }

    private static InstallAndUpgradeCodeunitsShouldBeInternalCodeAction CreateCodeAction(CodeunitSyntax codeunitSyntax, Document document, bool generateFixAll)
    {
        return new InstallAndUpgradeCodeunitsShouldBeInternalCodeAction(
            ApplicationCopAnalyzers.InstallAndUpgradeCodeunitsShouldBeInternalCodeAction,
            ct => SetAccessPropertyToInternal(document, codeunitSyntax, ct),
            nameof(InstallAndUpgradeCodeunitsShouldBeInternalCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> SetAccessPropertyToInternal(Document document, CodeunitSyntax codeunitSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var accessProperty = CreateAccessInternalProperty();

        var propertyList = codeunitSyntax.PropertyList;
        if (propertyList is null)
            return document;

        var properties = propertyList.Properties;

        var existingAccessProperty = properties
                                        .OfType<PropertySyntax>()
                                        .FirstOrDefault(p => string.Equals(
                                                                p.Name?.Identifier.ValueText,
                                                                AccessPropertyName,
                                                                StringComparison.OrdinalIgnoreCase));

        PropertyListSyntax newPropertyList;
        if (existingAccessProperty is not null)
        {
            // Replace value: Access = Internal;
            var updated = existingAccessProperty.WithValue(accessProperty.Value);
            newPropertyList = propertyList.WithProperties(properties.Replace(existingAccessProperty, updated));
        }
        else
        {
            // Add the Access property (at the beginning)
            newPropertyList = propertyList.WithProperties(properties.Insert(0, accessProperty));
        }

        var newRoot = root.ReplaceNode(propertyList, newPropertyList);
        return document.WithSyntaxRoot(newRoot);
    }

    private static PropertySyntax CreateAccessInternalProperty()
    {
        return SyntaxFactory.Property(
            AccessPropertyName,
            SyntaxFactory.EnumPropertyValue(SyntaxFactory.IdentifierName(InternalAccessValue)));
    }
}