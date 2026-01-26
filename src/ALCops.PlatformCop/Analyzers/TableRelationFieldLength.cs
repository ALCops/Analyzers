using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class TableRelationFieldLength : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.TableRelationFieldLength);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeSymbol,
            EnumProvider.SymbolKind.Field);

    private void AnalyzeSymbol(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IFieldSymbol field)
            return;

        if (!field.HasLength)
            return;

        var tableRelation = field
            .GetProperty(EnumProvider.PropertyKind.TableRelation)
            ?.GetPropertyValueSyntax<TableRelationPropertyValueSyntax>();

        if (tableRelation is null)
            return;

        AnalyzeTableRelations(ctx, field, tableRelation);
    }

    private void AnalyzeTableRelations(SymbolAnalysisContext ctx, IFieldSymbol field, TableRelationPropertyValueSyntax? tableRelation)
    {
        while (tableRelation is not null)
        {
            var relatedFieldSymbol = ResolveRelatedField(ctx, tableRelation);

            if (relatedFieldSymbol is not null && ShouldReportDiagnostic(field, relatedFieldSymbol))
            {
                ReportLengthMismatch(ctx, field, relatedFieldSymbol);
            }

            tableRelation = tableRelation.ElseExpression?.ElseTableRelationCondition;
        }
    }

    private static bool ShouldReportDiagnostic(IFieldSymbol currentField, IFieldSymbol relatedField) =>
        relatedField.HasLength && currentField.Length < relatedField.Length;

    private static void ReportLengthMismatch(SymbolAnalysisContext ctx, IFieldSymbol currentField, IFieldSymbol relatedField)
    {
        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.TableRelationFieldLength,
            currentField.GetLocation(),
            relatedField.Length,
#if NETSTANDARD2_1
            relatedField.ToDisplayStringWithReflection().QuoteIdentifierIfNeededWithReflection(),
#else
            relatedField.ToDisplayString().QuoteIdentifierIfNeededWithReflection(),
#endif
            currentField.Length,
#if NETSTANDARD2_1
            currentField.ToDisplayStringWithReflection().QuoteIdentifierIfNeededWithReflection()));
#else
            currentField.ToDisplayString().QuoteIdentifierIfNeededWithReflection()));
#endif
    }

    private IFieldSymbol? ResolveRelatedField(SymbolAnalysisContext ctx, TableRelationPropertyValueSyntax tableRelation)
    {
        return tableRelation.RelatedTableField switch
        {
            QualifiedNameSyntax qualifiedName =>
                ResolveQualifiedField(qualifiedName, ctx.Compilation),

            IdentifierNameSyntax identifierName =>
                ResolvePrimaryKeyField(identifierName.Identifier.ValueText?.UnquoteIdentifier(), ctx.Compilation),

            _ => null
        };
    }

    private static IFieldSymbol? ResolveQualifiedField(QualifiedNameSyntax qualifiedName, Compilation compilation)
    {
        // Without namespaces
        if (qualifiedName.Left is IdentifierNameSyntax tableNameSyntax &&
            qualifiedName.Right is IdentifierNameSyntax fieldNameSyntax)
        {
            var tableName = tableNameSyntax.GetIdentifierOrLiteralValue();
            var fieldName = fieldNameSyntax.GetIdentifierOrLiteralValue();

            if (!string.IsNullOrEmpty(tableName) && !string.IsNullOrEmpty(fieldName))
            {
                return GetFieldFromTable(tableName, fieldName, compilation)
                    ?? GetFieldFromTableExtension(tableName, fieldName, compilation);
            }
        }

        // With namespaces
        if (qualifiedName.Left is QualifiedNameSyntax qualifiedNameLeft &&
            qualifiedName.Right is IdentifierNameSyntax qualifiedNameRight)
        {
            var leftIdentifier = qualifiedNameRight.GetIdentifierOrLiteralValue();
            var rightIdentifier = qualifiedNameLeft.Right.GetIdentifierOrLiteralValue();

            if (!string.IsNullOrEmpty(rightIdentifier) && !string.IsNullOrEmpty(leftIdentifier))
            {
                IFieldSymbol? field = GetFieldFromTable(rightIdentifier, leftIdentifier, compilation)
                    ?? GetFieldFromTableExtension(rightIdentifier, leftIdentifier, compilation);

                if (field?.ContainingNamespace?.ToString() == qualifiedNameLeft.Left.ToString())
                    return field;

                // Try resolving the primary key field if previous lookup failed
                IFieldSymbol? primaryKeyField = ResolvePrimaryKeyField(leftIdentifier, compilation);
                if (primaryKeyField?.ContainingNamespace?.ToString() == qualifiedNameLeft.ToString())
                    return primaryKeyField;
            }
        }

        return null;
    }

    private static IFieldSymbol? ResolvePrimaryKeyField(string? tableName, Compilation compilation)
    {
        if (string.IsNullOrEmpty(tableName))
            return null;

#if NETSTANDARD2_1
        var tableSymbols = compilation.GetApplicationObjectTypeSymbolsByNameAcrossModules(EnumProvider.SymbolKind.Table, tableName);
#else
        var tableSymbols = compilation.GetApplicationObjectTypeSymbolsByNameAcrossModulesAndNamespaces(EnumProvider.SymbolKind.Table, tableName);
#endif
        return tableSymbols.FirstOrDefault() is ITableTypeSymbol table && table.PrimaryKey.Fields.Length == 1
            ? table.PrimaryKey.Fields[0]
            : null;
    }

    private static IFieldSymbol? GetFieldFromTable(string tableName, string fieldName, Compilation compilation)
    {
#if NETSTANDARD2_1
        var tableSymbols = compilation.GetApplicationObjectTypeSymbolsByNameAcrossModules(EnumProvider.SymbolKind.Table, tableName);
#else
        var tableSymbols = compilation.GetApplicationObjectTypeSymbolsByNameAcrossModulesAndNamespaces(EnumProvider.SymbolKind.Table, tableName);
#endif
        return tableSymbols.FirstOrDefault() is ITableTypeSymbol table
            ? table.Fields.FirstOrDefault(f => f.Name == fieldName)
            : null;
    }

    private static IFieldSymbol? GetFieldFromTableExtension(string tableName, string fieldName, Compilation compilation)
    {
        return compilation.GetDeclaredApplicationObjectSymbols()
            .OfType<ITableExtensionTypeSymbol>()
            .Where(ext => ext.Target?.Name == tableName)
            .SelectMany(ext => ext.AddedFields)
            .FirstOrDefault(field => field.Name == fieldName);
    }
}