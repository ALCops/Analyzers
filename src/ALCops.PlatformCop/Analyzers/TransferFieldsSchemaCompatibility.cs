using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
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
        var targetFields = BuildEffectiveFields(targetTable, tableExtensions, IsInitPrimaryKeyFieldsEnabled(invocation));

        if (sourceFields.IsEmpty || targetFields.IsEmpty)
            return;

        var sourceById = BuildFieldMapById(sourceFields);
        var targetById = BuildFieldMapById(targetFields);

        if (sourceById.Count == 0 || targetById.Count == 0)
            return;

        var hasTableRelationEntry = HasTableRelation(sourceTable);

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
                GetToDisplayStringSafe(targetById[minTypeMismatchId]),
                sourceById[minTypeMismatchId].Name.QuoteIdentifierIfNeededWithReflection(),
                targetById[minTypeMismatchId].Name.QuoteIdentifierIfNeededWithReflection()));
        }
    }

    private static void AnalyzeTableExtension(SymbolAnalysisContext ctx)
    {
        if (ctx.Symbol is not ITableExtensionTypeSymbol tableExtension)
            return;

        if (tableExtension.Target is not ITableTypeSymbol sourceTable)
            return;

        var relations = TryFindBySource(sourceTable);
        if (!relations.Any())
            return;

        foreach (var relation in relations)
        {
            AnalyzeTableExtensionForRelation(ctx, relation);
        }
    }

    private static void AnalyzeTableExtensionForRelation(SymbolAnalysisContext ctx, TableRelation relation)
    {
        var tableExtensions = GetCachedTableExtensions(ctx.Compilation);

        var sourceTableExtensions =
            tableExtensions
                .Where(te => te.Target is not null && te.Target.Name.Equals(relation.Source.Name))
                .SelectMany(x => x.AddedFields);

        var targetTableExtensions =
            tableExtensions
                .Where(te => te.Target is not null && te.Target.Name.Equals(relation.Target.Name))
                .SelectMany(x => x.AddedFields);

        if (!sourceTableExtensions.Any() || !targetTableExtensions.Any())
            return;

        var sourceById = BuildFieldMapById(sourceTableExtensions);
        var targetById = BuildFieldMapById(targetTableExtensions);

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

                ReportField(ctx, DiagnosticDescriptors.TransferFieldsTypeMismatch, sourceField, sourceField, targetField, id, relation.Source.Name, relation.Target.Name);
                ReportField(ctx, DiagnosticDescriptors.TransferFieldsTypeMismatch, targetField, sourceField, targetField, id, relation.Target.Name, relation.Source.Name);
            }

            if (nameMismatch)
            {
                var diagnosticId = DiagnosticDescriptors.TransferFieldsNameMismatch.Id;
                if (IsFieldSuppressed(diagnosticId, sourceField) || IsFieldSuppressed(diagnosticId, targetField))
                    continue;

                ReportField(ctx, DiagnosticDescriptors.TransferFieldsNameMismatch, sourceField, sourceField, targetField, id, relation.Source.Name, relation.Target.Name);
                ReportField(ctx, DiagnosticDescriptors.TransferFieldsNameMismatch, targetField, sourceField, targetField, id, relation.Target.Name, relation.Source.Name);
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

    private static bool IsInitPrimaryKeyFieldsEnabled(IInvocationExpression invocation)
    {
        if (invocation.Arguments.Length < 2)
            return true; // Default is true

        var constant = invocation.Arguments[1].Value.ConstantValue;
        return constant.HasValue && constant.Value is true;
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
        ImmutableArray<ITableExtensionTypeSymbol> allTableExtensions,
        bool includePrimaryKeyFields = true)
    {
        var pkFields =
            !includePrimaryKeyFields && table.PrimaryKey is not null
                ? table.PrimaryKey.Fields
                : default;

        var baseBuilder = ImmutableArray.CreateBuilder<IFieldSymbol>(table.Fields.Length);

        foreach (var field in table.Fields)
        {
            var id = field.Id;

            if (id == 0 || id >= 2_000_000_000)
                continue;

            if (!pkFields.IsDefaultOrEmpty &&
                pkFields.Any(pk => pk.Id == id))
                continue;

            baseBuilder.Add(field);
        }

        var extensionFields =
            allTableExtensions
                .Where(ext => SameApplicationObject(ext.Target, table))
                .SelectMany(ext => ext.AddedFields);

        return baseBuilder.ToImmutable();
    }

    private static Dictionary<int, IFieldSymbol> BuildFieldMapById(IEnumerable<IFieldSymbol> fields)
    {
        var map = fields is ICollection<IFieldSymbol> collection
            ? new Dictionary<int, IFieldSymbol>(collection.Count)
            : new Dictionary<int, IFieldSymbol>();

        foreach (var field in fields)
        {
            if (field.FieldClass != EnumProvider.FieldClassKind.Normal)
                continue;

            var id = field.Id;
            map[id] = field;
        }

        return map;
    }

    private static bool AreFieldTypesEquivalent(IFieldSymbol source, IFieldSymbol target)
    {
#if NETSTANDARD2_1
        var sourceType = source.OriginalDefinition.GetTypeSymbol();
        var targetType = target.OriginalDefinition.GetTypeSymbol();
#else
        var sourceType = source.Type;
        var targetType = target.Type;
#endif

        if (sourceType is null || targetType is null)
            return false;

        var sourceKind = sourceType.GetNavTypeKindSafe();
        var targetKind = targetType.GetNavTypeKindSafe();

        // Explicitly allow Enum → Integer assignments
        if (sourceKind == EnumProvider.NavTypeKind.Enum &&
            targetKind == EnumProvider.NavTypeKind.Integer)
        {
            return true;
        }

        if (sourceType is IApplicationObjectTypeSymbol &&
            targetType is IApplicationObjectTypeSymbol)
        {
            return SameApplicationObject(
                sourceType.OriginalDefinition,
                targetType.OriginalDefinition);
        }

        if (IsNumeric(sourceKind) && IsNumeric(targetKind))
        {
            return IsNumericAssignmentSafe(sourceKind, targetKind);
        }

        if (sourceType.HasLength && targetType.HasLength)
        {
            if (sourceType.Length > targetType.Length)
                return false;
        }

        // Explicitly allow Code → Text assignments
        if (sourceKind == EnumProvider.NavTypeKind.Code &&
            targetKind == EnumProvider.NavTypeKind.Text)
        {
            return true;
        }

        return sourceKind.Equals(targetKind);
    }

    private static bool IsNumeric(NavTypeKind kind)
    {
        return kind == EnumProvider.NavTypeKind.Integer
            || kind == EnumProvider.NavTypeKind.BigInteger
            || kind == EnumProvider.NavTypeKind.Decimal;
    }

    private static bool IsNumericAssignmentSafe(
        NavTypeKind source,
        NavTypeKind target)
    {
        if (source == target)
            return true;

        // Integer → BigInteger or Decimal is allowed
        if (source == EnumProvider.NavTypeKind.Integer &&
            (target == EnumProvider.NavTypeKind.BigInteger || target == EnumProvider.NavTypeKind.Decimal))
            return true;

        return false;
    }

    private static bool AreFieldNamesEquivalent(IFieldSymbol source, IFieldSymbol target)
    {
        var sourceName = (source.Name ?? string.Empty).UnquoteIdentifier();
        var targetName = (target.Name ?? string.Empty).UnquoteIdentifier();
        return string.Equals(sourceName, targetName, StringComparison.OrdinalIgnoreCase);
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
                GetToDisplayStringSafe(targetField),
                sourceField.Name.QuoteIdentifierIfNeededWithReflection(),
                targetField.Name.QuoteIdentifierIfNeededWithReflection()));
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
                GetToDisplayStringSafe(targetField),
                sourceField.Name.QuoteIdentifierIfNeededWithReflection(),
                targetField.Name.QuoteIdentifierIfNeededWithReflection()));
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

    private static bool SameApplicationObject(ISymbol? source, ISymbol? target)
    {
        if (source is null || target is null)
            return false;

        source = source.OriginalDefinition;
        target = target.OriginalDefinition;

        if (ReferenceEquals(source, target))
            return true;

        if (source is ISymbolWithId lId && target is ISymbolWithId rId)
            return lId.Id == rId.Id && source.Kind == target.Kind;

        return source.Equals(target);
    }
}