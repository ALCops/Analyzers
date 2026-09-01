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

    private static readonly Lazy<HashSet<NavTypeKind>> DataAccessNavTypeKinds = new(() =>
    [
        EnumProvider.NavTypeKind.Record,
        EnumProvider.NavTypeKind.RecordRef,
        EnumProvider.NavTypeKind.FieldRef,
        EnumProvider.NavTypeKind.Query,
        EnumProvider.NavTypeKind.Variant,
        EnumProvider.NavTypeKind.DataTransfer,
        EnumProvider.NavTypeKind.FilterPageBuilder,
        EnumProvider.NavTypeKind.TableFilter,
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

        // Suppress string literals used in Caption properties
        // Captions are user-facing text and may legitimately contain words that coincidentally match PageStyle names
        PropertyKind[] captionKinds = [EnumProvider.PropertyKind.Caption, EnumProvider.PropertyKind.OptionCaption];

        if (ctx.ContainingSymbol is IPropertySymbol ps && captionKinds.Contains(ps.PropertyKind))
            return;

        // Suppress enum and enum value contexts
        // Enum names and captions are identifiers, not style expressions and must not be constrained by PageStyle typing rules
        if (ctx.ContainingSymbol is ISymbol sym)
        {
            var k = sym.Kind;
            if (k == EnumProvider.SymbolKind.Enum || k == EnumProvider.SymbolKind.EnumValue)
                return;
        }

        if (stringLiteral.Value.Value is not string stringLiteralValue || stringLiteralValue.Length == 0)
            return;

        if (!StyleKindCanonicalNames.Value.TryGetValue(stringLiteralValue, out string? styleKind))
            return;

        // Allow for Label which is not locked (Locked is missing or false)
        var labelSyntax = GetLabelSyntax(stringLiteral);
        if (labelSyntax is not null)
        {
            bool isLocked = labelSyntax.GetBooleanPropertyValue(IdentifierProperty.Locked) == true;
            if (!isLocked)
                return;
        }

        // Suppress diagnostic on assigning StyleExpr property of a field
        if (IsStyleExprAssignment(ctx))
            return;

        // Suppress diagnostic on assignment of field on a table
        if (IsWritingToTableField(ctx))
            return;

        // Suppress diagnostic on string literals used as arguments to data-access method invocations
        // Methods on Record, RecordRef, FieldRef, Query, etc. operate on data values, not styles
        if (IsDataAccessMethodInvocationArgument(ctx))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PageStyleStringLiteral,
            ctx.Node.GetLocation(),
            stringLiteralValue,
            styleKind));
    }

    private static LabelSyntax? GetLabelSyntax(StringLiteralValueSyntax stringLiteralNode)
    {
        if (stringLiteralNode.GetFirstParent(EnumProvider.SyntaxKind.Label) is LabelSyntax parentNode)
            return parentNode;

        return null;
    }

    private static bool IsStyleExprAssignment(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.Node.GetFirstParent(EnumProvider.SyntaxKind.Property) is PropertySyntax propertyNode &&
            propertyNode.Value is StyleExpressionPropertyValueSyntax)
        {
            return true;
        }
        return false;
    }

    private static bool IsWritingToTableField(SyntaxNodeAnalysisContext ctx)
    {
        var assignmentNode = ctx.Node.GetFirstParent(EnumProvider.SyntaxKind.AssignmentStatement) as AssignmentStatementSyntax;
        if (assignmentNode?.Target is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression.IsKind(EnumProvider.SyntaxKind.IdentifierName))
        {
            var fieldSymbol = ctx.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol as IFieldSymbol;
            if (fieldSymbol is not null && fieldSymbol.ContainingType?.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Record)
                return true;
        }
        return false;
    }

    private static bool IsDataAccessMethodInvocationArgument(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.Node.GetFirstParent(EnumProvider.SyntaxKind.InvocationExpression)
            is not InvocationExpressionSyntax invocation)
            return false;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        var instanceSymbol = ctx.SemanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
        if (instanceSymbol?.GetTypeSymbol()?.GetNavTypeKindSafe() is not { } navTypeKind)
            return false;

        return DataAccessNavTypeKinds.Value.Contains(navTypeKind);
    }
}