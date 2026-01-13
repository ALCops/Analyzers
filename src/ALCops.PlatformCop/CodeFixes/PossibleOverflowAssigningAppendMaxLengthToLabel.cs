using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(PossibleOverflowAssigningAppendMaxLengthToLabelCodeFix))]
public sealed class PossibleOverflowAssigningAppendMaxLengthToLabelCodeFix : CodeFixProvider
{
#if NETSTANDARD2_1
    // C# 9 records require 'System.Runtime.CompilerServices.IsExternalInit' which doesn't exist in netstandard2.1.
    // We use a regular class for netstandard2.1 and a record for .NET 8+ to maintain compatibility with both targets.
    private sealed class CodeFixProperties
    {
        public int TargetLength { get; }
        public bool HasMaxLengthProperty { get; }

        private CodeFixProperties(int _targetLength, bool _hasMaxLengthProperty)
        {
            TargetLength = _targetLength;
            HasMaxLengthProperty = _hasMaxLengthProperty;
        }

        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(TargetLength), out var _targetLengthString) || string.IsNullOrEmpty(_targetLengthString) || !int.TryParse(_targetLengthString, out var _targetLength))
                return null;

            if (!properties.TryGetValue(nameof(HasMaxLengthProperty), out var _hasMaxLengthPropertyString) || string.IsNullOrEmpty(_hasMaxLengthPropertyString) || !bool.TryParse(_hasMaxLengthPropertyString, out var _hasMaxLengthProperty))
                return null;

            return new CodeFixProperties(_targetLength, _hasMaxLengthProperty);
        }
    }
#endif

#if NET8_0_OR_GREATER
    private sealed record CodeFixProperties(int TargetLength, bool HasMaxLengthProperty)
    {
        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(TargetLength), out var _targetLengthString) || string.IsNullOrEmpty(_targetLengthString) || !int.TryParse(_targetLengthString, out var _targetLength))
                return null;

            if (!properties.TryGetValue(nameof(HasMaxLengthProperty), out var _hasMaxLengthPropertyString) || string.IsNullOrEmpty(_hasMaxLengthPropertyString) || !bool.TryParse(_hasMaxLengthPropertyString, out var _hasMaxLengthProperty))
                return null;

            return new CodeFixProperties(_targetLength, _hasMaxLengthProperty);
        }
    }
#endif

    private class PossibleOverflowAssigningAppendMaxLengthToLabelCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public PossibleOverflowAssigningAppendMaxLengthToLabelCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.PossibleOverflowAssigning.Id);

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
                  .FirstOrDefault(d => d.Id == DiagnosticDescriptors.PossibleOverflowAssigning.Id);

        var properties = CodeFixProperties.TryParse(diagnostic?.Properties);
        if (properties is null)
            return;

        if (!properties.HasMaxLengthProperty)
            return;

        SyntaxNode node = syntaxRoot.FindNode(span);
        ctx.RegisterCodeFix(CreateCodeAction(node, document, properties, generateFixAll: true), ctx.Diagnostics[0]);
    }

    private static PossibleOverflowAssigningAppendMaxLengthToLabelCodeAction CreateCodeAction(SyntaxNode node, Document document, CodeFixProperties properties, bool generateFixAll)
    {
        return new PossibleOverflowAssigningAppendMaxLengthToLabelCodeAction(
            PlatformCopAnalyzers.PossibleOverflowAssigningAppendMaxLengthToLabelCodeAction,
            ct => AppendMaxLengthPropertyToLabel(document, node, properties, ct),
            nameof(PossibleOverflowAssigningAppendMaxLengthToLabelCodeFix),
            generateFixAll);
    }

    private static async Task<Document> AppendMaxLengthPropertyToLabel(Document document, SyntaxNode node, CodeFixProperties properties, CancellationToken cancellationToken)
    {
        Task<SemanticModel> semanticModelTask = document.GetSemanticModelAsync(cancellationToken);
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var semanticModel = await semanticModelTask.ConfigureAwait(false);
        if (semanticModel is null)
            return document;

        if (!TryFindLabelDeclaration(semanticModel, node, out var labelDeclaration, cancellationToken) || labelDeclaration is null)
            return document;

        var maxLengthProperty = CreateMaxLengthProperty(properties.TargetLength);
        var propertyList = labelDeclaration.Label.Properties ?? SyntaxFactory.CommaSeparatedIdentifierEqualsLiteralList();
        propertyList = propertyList.AddValues(maxLengthProperty);

        // Replace the properties or the entire label declaration if no properties existed
        SyntaxNode newLabelDeclaration = labelDeclaration.Label.Properties is not null
            ? labelDeclaration.ReplaceNode(labelDeclaration.Label.Properties, propertyList)
            : ReplaceOrAddPropertiesInLabelDataType(labelDeclaration, propertyList);

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(labelDeclaration, newLabelDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }

    private static bool TryFindLabelDeclaration(SemanticModel semanticModel, SyntaxNode node, out LabelDataTypeSyntax? labelDeclaration, CancellationToken cancellationToken)
    {
        labelDeclaration = null;

        // The diagnostic is reported on the label identifier, so let's find the variable symbol
        if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is not IVariableSymbol variableSymbol)
            return false;

        if (variableSymbol.DeclaringSyntaxReference?.GetSyntax(cancellationToken) is not VariableDeclarationSyntax variableSyntax)
            return false;

        // Check if it's a Label type
        if (variableSyntax.Type.DataType is not LabelDataTypeSyntax labelSyntax)
            return false;

        // Check if MaxLength property is already set - if so, don't offer the fix
        if (labelSyntax.GetIntegerPropertyValue(IdentifierProperty.MaxLength) is not null)
            return false;

        labelDeclaration = labelSyntax;
        return true;
    }

    private static LabelDataTypeSyntax ReplaceOrAddPropertiesInLabelDataType(LabelDataTypeSyntax original, CommaSeparatedIdentifierEqualsLiteralListSyntax propertyList)
    {
        // Create a new label with properties (and need to include an extra comma)
        var newLabel = SyntaxFactory.Label(original.Label.LabelText, SyntaxFactory.Token(EnumProvider.SyntaxKind.CommaToken), propertyList);

        // Create a new label data type with the updated label
        var newLabelDataType = SyntaxFactory.LabelDataType(SyntaxFactory.Token(EnumProvider.SyntaxKind.LabelKeyword), newLabel);

        // Find the original label keyword token and replace the new one with the original to preserve casing
        var originalKeywordToken = original.DescendantTokens().FirstOrDefault(t => t.IsKind(EnumProvider.SyntaxKind.LabelKeyword));
        var newKeywordToken = newLabelDataType.DescendantTokens().FirstOrDefault(t => t.IsKind(EnumProvider.SyntaxKind.LabelKeyword));

        if (!originalKeywordToken.IsKind(EnumProvider.SyntaxKind.None) && !newKeywordToken.IsKind(EnumProvider.SyntaxKind.None))
        {
            newLabelDataType = newLabelDataType.ReplaceToken(newKeywordToken, originalKeywordToken);
        }

        return newLabelDataType;
    }

    private static IdentifierEqualsLiteralSyntax CreateMaxLengthProperty(int maxLength)
    {
        var identifier = SyntaxFactory.Identifier(LabelPropertyHelper.MaxLength);
        var literalValue = SyntaxFactory.Int32SignedLiteralValue(SyntaxFactory.Literal(maxLength));

        return SyntaxFactory.IdentifierEqualsLiteral(identifier, literalValue);
    }
}