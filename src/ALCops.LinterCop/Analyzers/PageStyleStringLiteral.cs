using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PageStyleStringLiteral : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.PageStyleStringLiteral
        );

    public override VersionCompatibility SupportedVersions =>
        VersionProvider.VersionCompatibility.Fall2024OrGreater;

    // Case-SENSITIVE dictionary (StringComparer.Ordinal) to match only exact PascalCase StyleKind values.
    // EnumProvider.StyleKind.CanonicalNames uses OrdinalIgnoreCase, which causes false positives
    // on all-caps constants like 'STANDARD' or 'NONE' (see issue #183).
    private static readonly Lazy<ImmutableDictionary<string, string>> StyleKindCanonicalNames =
        new(() => Enum.GetNames(typeof(Microsoft.Dynamics.Nav.CodeAnalysis.StyleKind))
            .ToImmutableDictionary(
                name => name,
                name => name,
                StringComparer.Ordinal),
            LazyThreadSafetyMode.PublicationOnly);

    private static readonly Lazy<HashSet<SyntaxKind>> ComparisonKinds = new(() =>
    [
        EnumProvider.SyntaxKind.EqualsExpression,
        EnumProvider.SyntaxKind.NotEqualsExpression,
        EnumProvider.SyntaxKind.LessThanExpression,
        EnumProvider.SyntaxKind.LessThanOrEqualExpression,
        EnumProvider.SyntaxKind.GreaterThanExpression,
        EnumProvider.SyntaxKind.GreaterThanOrEqualExpression,
    ], LazyThreadSafetyMode.PublicationOnly);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            AnalyzeStringLiteralToken,
            EnumProvider.SyntaxKind.StringLiteralValue
        );

    private static void AnalyzeStringLiteralToken(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Node is not StringLiteralValueSyntax stringLiteral)
            return;

        if (stringLiteral.Value.Value is not string stringLiteralValue || stringLiteralValue.Length == 0)
            return;

        if (!StyleKindCanonicalNames.Value.TryGetValue(stringLiteralValue, out string? styleKind))
            return;

        // Suppress enum and enum value contexts
        // Enum names and captions are identifiers, not style expressions and must not be constrained by PageStyle typing rules
        if (ctx.ContainingSymbol is ISymbol sym)
        {
            var k = sym.Kind;
            if (k == EnumProvider.SymbolKind.Enum || k == EnumProvider.SymbolKind.EnumValue)
                return;
        }

        if (IsDeniedContext(stringLiteral))
            return;

        // Report only the text of a locked Label or report label (Locked = true); the text of an unlocked label
        // is translatable, and its other literals (Comment) are notes for translators.
        // A report label wraps the same LabelSyntax node as a Label variable
        var labelSyntax = GetLabelSyntax(stringLiteral);
        if (labelSyntax is not null)
        {
            if (labelSyntax.LabelText != stringLiteral)
                return;

            bool isLocked = labelSyntax.GetBooleanPropertyValue(IdentifierProperty.Locked) == true;
            if (!isLocked)
                return;
        }

        // Suppress diagnostic on assignment of field on a table
        if (IsWritingToTableField(ctx))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PageStyleStringLiteral,
            ctx.Node.GetLocation(),
            stringLiteralValue,
            styleKind));
    }

    // Contexts where a matching literal is data or text in every case, so the rule never reports there:
    // property values (Caption, Description, StyleExpr itself), arguments of any method call or attribute,
    // multilanguage text (TextConst and legacy report labels), case labels, and operands of comparisons and in-lists.
    private static bool IsDeniedContext(StringLiteralValueSyntax stringLiteral)
    {
        // One walk for the ancestor contexts. A multilanguage list (ENU = '...', in TextConst and in the legacy
        // label(Name; ENU = '...') report label) is translatable text and has no Locked option.
        foreach (var ancestor in stringLiteral.Ancestors())
        {
            var kind = ancestor.Kind;
            if (kind == EnumProvider.SyntaxKind.Property ||
                kind == EnumProvider.SyntaxKind.ArgumentList ||
                kind == EnumProvider.SyntaxKind.AttributeArgumentList ||
                kind == EnumProvider.SyntaxKind.CommaSeparatedIdentifierEqualsStringList)
                return true;
        }

        // In code the literal is wrapped in a LiteralExpression; parentheses, a range ('A'..'Z') and the branches
        // of a ternary wrap that again. The ternary's condition starts at the ternary itself, its branches after it.
        SyntaxNode? expression = stringLiteral.Parent;
        while (expression?.Parent is { } wrapper &&
            (wrapper.Kind == EnumProvider.SyntaxKind.ParenthesizedExpression ||
             wrapper.Kind == EnumProvider.SyntaxKind.RangeExpression ||
             (IsConditionalExpression(wrapper) && expression.SpanStart != wrapper.SpanStart)))
            expression = wrapper;

        if (expression?.Parent is not { } parent)
            return false;

        // Only the case line's values are its direct children; its statement is wrapped in a statement node
        if (parent.Kind == EnumProvider.SyntaxKind.CaseLine)
            return true;

        if (ComparisonKinds.Value.Contains(parent.Kind))
            return true;

        return parent.Kind == EnumProvider.SyntaxKind.BracketedArgumentList &&
            parent.Parent?.Kind == EnumProvider.SyntaxKind.InListExpression;
    }

    // The ternary kind does not exist before the 14.0 SDK; EnumProvider then resolves it to SyntaxKind.None
    private static bool IsConditionalExpression(SyntaxNode node) =>
        EnumProvider.SyntaxKind.ConditionalExpression != default &&
        node.Kind == EnumProvider.SyntaxKind.ConditionalExpression;

    private static LabelSyntax? GetLabelSyntax(StringLiteralValueSyntax stringLiteralNode)
    {
        if (stringLiteralNode.GetFirstParent(EnumProvider.SyntaxKind.Label) is LabelSyntax parentNode)
            return parentNode;

        return null;
    }

    private static bool IsWritingToTableField(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.Node.GetFirstParent(EnumProvider.SyntaxKind.AssignmentStatement) is not AssignmentStatementSyntax assignmentNode)
            return false;

        SyntaxNode? fieldNameNode = assignmentNode.Target switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            IdentifierNameSyntax identifierName => identifierName,
            _ => null
        };

        if (fieldNameNode is null)
            return false;

        var fieldSymbol = ctx.SemanticModel.GetSymbolInfo(fieldNameNode).Symbol as IFieldSymbol;
        return fieldSymbol is not null && fieldSymbol.ContainingType?.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Record;
    }
}