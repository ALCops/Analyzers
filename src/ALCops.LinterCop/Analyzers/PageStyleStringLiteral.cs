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

        // Allow for a Label or report label which is not locked (Locked is missing or false)
        // A report label wraps the same LabelSyntax node as a Label variable
        var labelSyntax = GetLabelSyntax(stringLiteral);
        if (labelSyntax is not null)
        {
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
    // property values (Caption, Description, StyleExpr itself), arguments of any method call,
    // legacy multilanguage report labels, case labels, and operands of comparisons and in-lists.
    private static bool IsDeniedContext(StringLiteralValueSyntax stringLiteral)
    {
        if (stringLiteral.GetFirstParent(EnumProvider.SyntaxKind.Property) is not null ||
            stringLiteral.GetFirstParent(EnumProvider.SyntaxKind.ArgumentList) is not null)
            return true;

        // A legacy multilanguage report label (label(Name; ENU = '...')) is translatable text and has no Locked option
        if (stringLiteral.GetFirstParent(EnumProvider.SyntaxKind.ReportLabelMultilanguage) is not null)
            return true;

        // In code the literal is wrapped in a LiteralExpression; parentheses and a range ('A'..'Z') wrap that again
        SyntaxNode? expression = stringLiteral.Parent;
        while (expression?.Parent is { } wrapper &&
            (wrapper.Kind == EnumProvider.SyntaxKind.ParenthesizedExpression ||
             wrapper.Kind == EnumProvider.SyntaxKind.RangeExpression))
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