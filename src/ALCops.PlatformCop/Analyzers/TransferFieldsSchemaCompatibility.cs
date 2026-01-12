using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
#if NETSTANDARD2_1
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
#endif
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;
using static ALCops.PlatformCop.Helpers.TransferFieldsRelations;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class TransferFieldsSchemaCompatibility : DiagnosticAnalyzer
{
    private const string TransferFieldsMethodName = "TransferFields";

    // Cache TableExtensionTypeSymbols per Compilation (to avoid repeated expensive queries)
    private static ImmutableArray<ITableExtensionTypeSymbol> GetCachedTableExtensions(Compilation compilation)
        => TableExtensionsCache.GetValue(compilation, static c => new TableExtensionsCacheEntry(c)).Value.Value;
    private static readonly ConditionalWeakTable<Compilation, TableExtensionsCacheEntry> TableExtensionsCache = new();
    private sealed class TableExtensionsCacheEntry(Compilation compilation)
    {
        public Lazy<ImmutableArray<ITableExtensionTypeSymbol>> Value { get; } =
            new Lazy<ImmutableArray<ITableExtensionTypeSymbol>>(
                () => compilation
                    .GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection(EnumProvider.SymbolKind.TableExtension)
                    .OfType<ITableExtensionTypeSymbol>()
                    .ToImmutableArray(),
                LazyThreadSafetyMode.ExecutionAndPublication);
    }

    // Cache CompilationPaths per Compilation (to avoid repeated expensive queries)
    private static HashSet<string> GetCachedCompilationPaths(Compilation compilation)
        => CompilationPathsCache.GetValue(compilation, static c => new CompilationPathsCacheEntry(c)).Paths.Value;
    private static readonly ConditionalWeakTable<Compilation, CompilationPathsCacheEntry> CompilationPathsCache = new();
    private sealed class CompilationPathsCacheEntry(Compilation compilation)
    {
        public Lazy<HashSet<string>> Paths { get; } = new Lazy<HashSet<string>>(
                () =>
                {
                    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var tree in compilation.SyntaxTrees)
                    {
                        var path = tree.FilePath;
                        if (!string.IsNullOrWhiteSpace(path))
                            set.Add(path);
                    }

                    return set;
                },
                LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.TransferFieldsNameMismatch,
            DiagnosticDescriptors.TransferFieldsTypeMismatch);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);

        context.RegisterSymbolAction(
            AnalyzeTableExtension,
            EnumProvider.SymbolKind.TableExtension);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.Operation is not IInvocationExpression invocation)
            return;

        var targetMethod = invocation.TargetMethod;
        if (targetMethod is null || targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        if (!string.Equals(targetMethod.Name, TransferFieldsMethodName, StringComparison.Ordinal))
            return;

        if (IsSkipFieldsNotMatchingTypeEnabled(invocation))
            return;

        var sourceTable =
            TryResolveSymbolFromArgument(invocation) as ITableTypeSymbol;

        var targetTable =
            invocation.Instance?.Type.OriginalDefinition as ITableTypeSymbol
            ?? ctx.ContainingSymbol.GetContainingApplicationObjectTypeSymbol()?.OriginalDefinition as ITableTypeSymbol;

        if (sourceTable is null || targetTable is null)
            return;

        var tableExtensions = GetCachedTableExtensions(ctx.Compilation);
        var sourceFields = BuildEffectiveFields(sourceTable, tableExtensions);
        var targetFields = BuildEffectiveFields(targetTable, tableExtensions);

        if (sourceFields.IsEmpty || targetFields.IsEmpty)
            return;

        var sourceById = BuildFieldMapById(sourceFields);
        var targetById = BuildFieldMapById(targetFields);

        if (sourceById.Count == 0 || targetById.Count == 0)
            return;

        var hasTableRelationEntry = TryFindTableRelation(sourceTable) is not null;

        var hasTypeMismatch = false;
        var hasNameMismatch = false;

        var minTypeMismatchId = int.MaxValue;
        var minNameMismatchId = int.MaxValue;

        foreach (var kvp in sourceById)
        {
            var id = kvp.Key;
            if (!targetById.TryGetValue(id, out var targetField))
                continue;

            var sourceField = kvp.Value;

            var typeMismatch = !AreFieldTypesEquivalent(sourceField, targetField);
            var nameMismatch = !AreFieldNamesEquivalent(sourceField, targetField);

            if (typeMismatch)
            {
                var diagnosticId = DiagnosticDescriptors.TransferFieldsTypeMismatch.Id;

                if (!(IsFieldSuppressed(diagnosticId, sourceField) || IsFieldSuppressed(diagnosticId, targetField)))
                {
                    hasTypeMismatch = true;
                    if (id < minTypeMismatchId)
                        minTypeMismatchId = id;

                    if (!hasTableRelationEntry)
                    {
                        ReportField(ctx, DiagnosticDescriptors.TransferFieldsTypeMismatch, locationField: sourceField, sourceField, targetField, id, sourceTable, targetTable);
                        ReportField(ctx, DiagnosticDescriptors.TransferFieldsTypeMismatch, locationField: targetField, sourceField, targetField, id, sourceTable, targetTable);
                    }
                }
            }

            if (nameMismatch)
            {
                var diagnosticId = DiagnosticDescriptors.TransferFieldsNameMismatch.Id;

                if (!(IsFieldSuppressed(diagnosticId, sourceField) || IsFieldSuppressed(diagnosticId, targetField)))
                {

                    hasNameMismatch = true;
                    if (id < minNameMismatchId)
                        minNameMismatchId = id;

                    if (!hasTableRelationEntry)
                    {
                        ReportField(ctx, DiagnosticDescriptors.TransferFieldsNameMismatch, locationField: sourceField, sourceField, targetField, id, sourceTable, targetTable);
                        ReportField(ctx, DiagnosticDescriptors.TransferFieldsNameMismatch, locationField: targetField, sourceField, targetField, id, sourceTable, targetTable);
                    }
                }
            }
        }

        if (!hasTypeMismatch && !hasNameMismatch)
            return;

        var targetDisplay = targetTable.GetFullyQualifiedObjectName(quoteIdentifierIfNeeded: true);
        var sourceDisplay = sourceTable.GetFullyQualifiedObjectName(quoteIdentifierIfNeeded: true);

        if (hasNameMismatch)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TransferFieldsNameMismatch,
                ctx.Operation.Syntax.GetLocation(),
                minNameMismatchId,
                sourceDisplay,
                targetDisplay,
                sourceById[minNameMismatchId].Name.QuoteIdentifierIfNeededWithReflection(),
                targetById[minNameMismatchId].Name.QuoteIdentifierIfNeededWithReflection()));
        }

        if (hasTypeMismatch)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TransferFieldsTypeMismatch,
                ctx.Operation.Syntax.GetLocation(),
                minTypeMismatchId,
                sourceDisplay,
                targetDisplay,
                GetToDisplayStringSafe(sourceById[minTypeMismatchId]),
                GetToDisplayStringSafe(targetById[minTypeMismatchId])));
        }
    }

    private static void AnalyzeTableExtension(SymbolAnalysisContext ctx)
    {
        if (ctx.Symbol is not ITableExtensionTypeSymbol tableExtension)
            return;

        if (tableExtension.Target is not ITableTypeSymbol sourceTable)
            return;

        if (TryFindTableRelation(sourceTable) is not { } tableRelation)
            return;

        var targetObjectName =
            tableRelation.Table.Name == sourceTable.Name ? tableRelation.RelatedTable :
            tableRelation.RelatedTable.Name == sourceTable.Name ? tableRelation.Table :
            throw new InvalidOperationException("Source table not part of relation.");

        var tableExtensions = GetCachedTableExtensions(ctx.Compilation);

        var sourceTableExtensions =
            tableExtensions
                .Where(te => te.Target is not null && te.Target.Name.Equals(sourceTable.Name))
                .SelectMany(x => x.AddedFields);

        var targetTableExtensions =
            tableExtensions
                .Where(te => te.Target is not null && te.Target.Name.Equals(targetObjectName.Name))
                .SelectMany(x => x.AddedFields);

        if (!sourceTableExtensions.Any() || !targetTableExtensions.Any())
            return;

        var sourceById = BuildFieldMapById(sourceTableExtensions);
        var targetById = BuildFieldMapById(targetTableExtensions);

        if (sourceById.Count == 0 || targetById.Count == 0)
            return;

        foreach (var kvp in sourceById)
        {
            var id = kvp.Key;
            if (!targetById.TryGetValue(id, out var targetField))
                continue;

            var sourceField = kvp.Value;

            var typeMismatch = !AreFieldTypesEquivalent(sourceField, targetField);
            var nameMismatch = !AreFieldNamesEquivalent(sourceField, targetField);

            if (typeMismatch)
            {
                var diagnosticId = DiagnosticDescriptors.TransferFieldsTypeMismatch.Id;
                if (IsFieldSuppressed(diagnosticId, sourceField) || IsFieldSuppressed(diagnosticId, targetField))
                    continue;

                ReportField(ctx, DiagnosticDescriptors.TransferFieldsTypeMismatch, sourceField, sourceField, targetField, id, sourceTable.GetFullyQualifiedObjectName(true), targetObjectName.ToString());
                ReportField(ctx, DiagnosticDescriptors.TransferFieldsTypeMismatch, targetField, sourceField, targetField, id, targetObjectName.ToString(), sourceTable.GetFullyQualifiedObjectName(true));
            }

            if (nameMismatch)
            {
                var diagnosticId = DiagnosticDescriptors.TransferFieldsNameMismatch.Id;
                if (IsFieldSuppressed(diagnosticId, sourceField) || IsFieldSuppressed(diagnosticId, targetField))
                    continue;

                ReportField(ctx, DiagnosticDescriptors.TransferFieldsNameMismatch, sourceField, sourceField, targetField, id, sourceTable.GetFullyQualifiedObjectName(true), targetObjectName.ToString());
                ReportField(ctx, DiagnosticDescriptors.TransferFieldsNameMismatch, targetField, sourceField, targetField, id, targetObjectName.ToString(), sourceTable.GetFullyQualifiedObjectName(true));
            }
        }
    }

    private static bool IsFieldSuppressed(string diagnosticId, IFieldSymbol field)
    {
        var fieldSyntax = field.DeclaringSyntaxReference?.GetSyntax();
        if (fieldSyntax is null)
            return false;

        foreach (var pragma in fieldSyntax.GetDirectives().OfType<PragmaWarningDirectiveTriviaSyntax>())
        {
            foreach (var code in pragma.ErrorCodes.OfType<IdentifierNameSyntax>())
            {
                if (code.Identifier.Text.Contains(diagnosticId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private static bool IsSkipFieldsNotMatchingTypeEnabled(IInvocationExpression invocation)
    {
        if (invocation.Arguments.Length < 3)
            return false;

        var constant = invocation.Arguments[2].Value.ConstantValue;
        return constant.HasValue && constant.Value is true;
    }

    private static ISymbol? TryResolveSymbolFromArgument(IInvocationExpression invocation)
    {
        if (invocation.Arguments.Length < 1)
            return null;

        var value = invocation.Arguments[0].Value;

        if (value is IConversionExpression conv)
            return conv.Operand.Type?.OriginalDefinition;

        return value.Type?.OriginalDefinition;
    }

    private static ImmutableArray<IFieldSymbol> BuildEffectiveFields(
        ITableTypeSymbol table,
        ImmutableArray<ITableExtensionTypeSymbol> allTableExtensions)
    {
        var baseFields = table.Fields;

        var extensionFields =
            allTableExtensions
                .Where(ext => SameApplicationObject(ext.Target, table))
                .SelectMany(ext => ext.AddedFields)
                .ToImmutableArray();

        if (extensionFields.IsEmpty)
            return baseFields;

        return baseFields.AddRange(extensionFields);
    }

    private static Dictionary<int, IFieldSymbol> BuildFieldMapById(IEnumerable<IFieldSymbol> fields)
    {
        var map = new Dictionary<int, IFieldSymbol>();

        foreach (var field in fields)
        {
            if (field is ISymbolWithId withId)
            {
                var id = (int)withId.Id;
                if (id >= 2000000000)
                    continue;

                if (field.FieldClass != EnumProvider.FieldClassKind.Normal)
                    continue;

                map[id] = field;
            }
        }

        return map;
    }

    private static bool AreFieldTypesEquivalent(IFieldSymbol left, IFieldSymbol right)
    {
#if NETSTANDARD2_1
        var lt = left.OriginalDefinition.GetTypeSymbol();
        var rt = right.OriginalDefinition.GetTypeSymbol();
#else
        var lt = left.Type;
        var rt = right.Type;
#endif
        if (lt is null || rt is null)
            return false;

        if (lt is IApplicationObjectTypeSymbol && rt is IApplicationObjectTypeSymbol)
            return SameApplicationObject(lt.OriginalDefinition, rt.OriginalDefinition);

#if NETSTANDARD2_1
        return string.Equals(lt.ToDisplayStringWithReflection(), rt.ToDisplayStringWithReflection(), StringComparison.OrdinalIgnoreCase);
#else
        return string.Equals(lt.ToDisplayString(), rt.ToDisplayString(), StringComparison.OrdinalIgnoreCase);
#endif
    }

    private static bool AreFieldNamesEquivalent(IFieldSymbol left, IFieldSymbol right)
    {
        var leftName = (left.Name ?? string.Empty).UnquoteIdentifier();
        var rightName = (right.Name ?? string.Empty).UnquoteIdentifier();
        return string.Equals(leftName, rightName, StringComparison.OrdinalIgnoreCase);
    }

    private static void ReportField(
        SymbolAnalysisContext ctx,
        DiagnosticDescriptor descriptor,
        IFieldSymbol locationField,
        IFieldSymbol sourceField,
        IFieldSymbol targetField,
        int fieldId,
        string sourceDisplay,
        string targetDisplay)
    {
        var location = locationField.DeclaringSyntaxReference?.GetSyntax().GetLocation();
        if (location is null)
            return;

        if (!location.IsInSource || !IsLocationInCompilation(location, ctx.Compilation))
            return;

        if (descriptor.Id == DiagnosticDescriptors.TransferFieldsNameMismatch.Id)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                location,
                fieldId,
                sourceDisplay,
                targetDisplay,
                sourceField.Name.QuoteIdentifierIfNeededWithReflection(),
                targetField.Name.QuoteIdentifierIfNeededWithReflection()));
            return;
        }

        if (descriptor.Id == DiagnosticDescriptors.TransferFieldsTypeMismatch.Id)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                location,
                fieldId,
                sourceDisplay,
                targetDisplay,
                GetToDisplayStringSafe(sourceField),
                GetToDisplayStringSafe(targetField)));
            return;
        }
    }

    private static void ReportField(
        OperationAnalysisContext ctx,
        DiagnosticDescriptor descriptor,
        IFieldSymbol locationField,
        IFieldSymbol sourceField,
        IFieldSymbol targetField,
        int fieldId,
        ITableTypeSymbol sourceTable,
        ITableTypeSymbol targetTable)
    {
        var location = locationField.Location;
        if (location is null)
            return;

        if (!location.IsInSource || !IsLocationInCompilation(location, ctx.Compilation))
            return;

        var sourceDisplay = sourceTable.GetFullyQualifiedObjectName(quoteIdentifierIfNeeded: true);
        var targetDisplay = targetTable.GetFullyQualifiedObjectName(quoteIdentifierIfNeeded: true);

        if (descriptor.Id == DiagnosticDescriptors.TransferFieldsNameMismatch.Id)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                location,
                fieldId,
                sourceDisplay,
                targetDisplay,
                sourceField.Name.QuoteIdentifierIfNeededWithReflection(),
                targetField.Name.QuoteIdentifierIfNeededWithReflection()));
            return;
        }

        if (descriptor.Id == DiagnosticDescriptors.TransferFieldsTypeMismatch.Id)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                location,
                fieldId,
                sourceDisplay,
                targetDisplay,
                GetToDisplayStringSafe(sourceField),
                GetToDisplayStringSafe(targetField)));
            return;
        }
    }

    private static string GetToDisplayStringSafe(IFieldSymbol fieldSymbol)
    {
#if NETSTANDARD2_1
        return fieldSymbol.OriginalDefinition.GetTypeSymbol().ToDisplayStringWithReflection() ?? EnumProvider.NavTypeKind.None.ToString();
#else
        return fieldSymbol.Type?.ToDisplayString() ?? EnumProvider.NavTypeKind.None.ToString();
#endif
    }

    private static bool IsLocationInCompilation(Location location, Compilation compilation)
    {
        var path = location.SourceTree?.FilePath;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return GetCachedCompilationPaths(compilation).Contains(path);
    }

    private static bool SameApplicationObject(ISymbol? left, ISymbol? right)
    {
        if (left is null || right is null)
            return false;

        left = left.OriginalDefinition;
        right = right.OriginalDefinition;

        if (ReferenceEquals(left, right))
            return true;

        if (left is ISymbolWithId lId && right is ISymbolWithId rId)
            return lId.Id == rId.Id && left.Kind == right.Kind;

        return left.Equals(right);
    }
}