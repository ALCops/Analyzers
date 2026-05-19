using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;
using static ALCops.PlatformCop.Helpers.TransferFieldsRelations;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class TransferFieldsSchemaCompatibility : DiagnosticAnalyzer
{
    private const string TransferFieldsMethodName = "TransferFields";

    [Flags]
    private enum MismatchKind { None = 0, Type = 1, Name = 2 }

#if NETSTANDARD2_1
    private readonly struct FieldMismatch
    {
        public int FieldId { get; }
        public IFieldSymbol Source { get; }
        public IFieldSymbol Target { get; }
        public MismatchKind Kind { get; }

        public FieldMismatch(int fieldId, IFieldSymbol source, IFieldSymbol target, MismatchKind kind)
        {
            FieldId = fieldId;
            Source = source;
            Target = target;
            Kind = kind;
        }
    }

    private readonly struct MismatchResult
    {
        public bool HasTypeMismatch { get; }
        public bool HasNameMismatch { get; }
        public int MinTypeId { get; }
        public int MinNameId { get; }

        public MismatchResult(bool hasTypeMismatch, bool hasNameMismatch, int minTypeId, int minNameId)
        {
            HasTypeMismatch = hasTypeMismatch;
            HasNameMismatch = hasNameMismatch;
            MinTypeId = minTypeId;
            MinNameId = minNameId;
        }
    }
#else
    private readonly record struct FieldMismatch(int FieldId, IFieldSymbol Source, IFieldSymbol Target, MismatchKind Kind);
    private readonly record struct MismatchResult(bool HasTypeMismatch, bool HasNameMismatch, int MinTypeId, int MinNameId);
#endif

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

        var targetDisplay = targetTable.GetFullyQualifiedObjectName(quoteIdentifierIfNeeded: true);
        var sourceDisplay = sourceTable.GetFullyQualifiedObjectName(quoteIdentifierIfNeeded: true);

        var mismatches = FindFieldMismatches(sourceById, targetById);

        var result = ReportMismatches(
            mismatches,
            ctx.Compilation,
            sourceDisplay,
            targetDisplay,
            reportAtFieldLevel: !hasTableRelationEntry,
            swapDisplayForTarget: false,
            getLocation: static field => field.Location,
            reportDiagnostic: ctx.ReportDiagnostic);

        if (!result.HasTypeMismatch && !result.HasNameMismatch)
            return;

        if (result.HasNameMismatch)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TransferFieldsNameMismatch,
                ctx.Operation.Syntax.GetLocation(),
                result.MinNameId,
                sourceDisplay,
                targetDisplay,
                sourceById[result.MinNameId].Name.QuoteIdentifierIfNeededWithReflection(),
                targetById[result.MinNameId].Name.QuoteIdentifierIfNeededWithReflection()));
        }

        if (result.HasTypeMismatch)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TransferFieldsTypeMismatch,
                ctx.Operation.Syntax.GetLocation(),
                result.MinTypeId,
                sourceDisplay,
                targetDisplay,
                GetToDisplayStringSafe(sourceById[result.MinTypeId]),
                GetToDisplayStringSafe(targetById[result.MinTypeId]),
                sourceById[result.MinTypeId].Name.QuoteIdentifierIfNeededWithReflection(),
                targetById[result.MinTypeId].Name.QuoteIdentifierIfNeededWithReflection()));
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

        var sourceById = BuildFieldMapById(sourceTableExtensions);
        var targetById = BuildFieldMapById(targetTableExtensions);

        if (sourceById.Count == 0 || targetById.Count == 0)
            return;

        var mismatches = FindFieldMismatches(sourceById, targetById);

        ReportMismatches(
            mismatches,
            ctx.Compilation,
            relation.Source.Name,
            relation.Target.Name,
            reportAtFieldLevel: true,
            swapDisplayForTarget: true,
            getLocation: static field => field.DeclaringSyntaxReference?.GetSyntax().GetLocation(),
            reportDiagnostic: ctx.ReportDiagnostic);
    }

    private static List<FieldMismatch> FindFieldMismatches(
        Dictionary<int, IFieldSymbol> sourceById,
        Dictionary<int, IFieldSymbol> targetById)
    {
        var mismatches = new List<FieldMismatch>();

        foreach (var kvp in sourceById)
        {
            var id = kvp.Key;
            if (!targetById.TryGetValue(id, out var targetField))
                continue;

            var sourceField = kvp.Value;
            var kind = MismatchKind.None;

            if (!AreFieldTypesEquivalent(sourceField, targetField))
                kind |= MismatchKind.Type;

            if (!AreFieldNamesEquivalent(sourceField, targetField))
                kind |= MismatchKind.Name;

            if (kind != MismatchKind.None)
                mismatches.Add(new FieldMismatch(id, sourceField, targetField, kind));
        }

        return mismatches;
    }

    private static bool IsEitherFieldSuppressed(string diagnosticId, IFieldSymbol source, IFieldSymbol target)
        => IsFieldSuppressed(diagnosticId, source) || IsFieldSuppressed(diagnosticId, target);

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

        var extensionFields =
            allTableExtensions
                .Where(ext => SameApplicationObject(ext.Target, table))
                .SelectMany(ext => ext.AddedFields);

        IEnumerable<IFieldSymbol> baseFields = table.Fields;
        if (!pkFields.IsDefaultOrEmpty)
            baseFields = baseFields.Where(f => !pkFields.Any(pk => pk.Id == f.Id));

        var allFields = baseFields.Concat(extensionFields);

        var builder = ImmutableArray.CreateBuilder<IFieldSymbol>();

        // Note: IsRemoved() filtering is handled by BuildFieldMapById
        foreach (var field in allFields)
        {
            var id = field.Id;

            if (id == 0 || id >= 2_000_000_000)
                continue;

            builder.Add(field);
        }

        return builder.ToImmutable();
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

            if (field.IsRemoved())
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

    private static MismatchResult ReportMismatches(
        List<FieldMismatch> mismatches,
        Compilation compilation,
        string sourceDisplay,
        string targetDisplay,
        bool reportAtFieldLevel,
        bool swapDisplayForTarget,
        Func<IFieldSymbol, Location?> getLocation,
        Action<Diagnostic> reportDiagnostic)
    {
        var hasTypeMismatch = false;
        var hasNameMismatch = false;
        var minTypeId = int.MaxValue;
        var minNameId = int.MaxValue;

        foreach (var mismatch in mismatches)
        {
            if (mismatch.Kind.HasFlag(MismatchKind.Type)
                && !IsEitherFieldSuppressed(DiagnosticDescriptors.TransferFieldsTypeMismatch.Id, mismatch.Source, mismatch.Target))
            {
                hasTypeMismatch = true;
                if (mismatch.FieldId < minTypeId)
                    minTypeId = mismatch.FieldId;

                if (reportAtFieldLevel)
                {
                    ReportFieldTypeMismatch(compilation, getLocation(mismatch.Source), mismatch.Source, mismatch.Target, mismatch.FieldId, sourceDisplay, targetDisplay, reportDiagnostic);
                    ReportFieldTypeMismatch(compilation, getLocation(mismatch.Target), mismatch.Source, mismatch.Target, mismatch.FieldId,
                        swapDisplayForTarget ? targetDisplay : sourceDisplay,
                        swapDisplayForTarget ? sourceDisplay : targetDisplay,
                        reportDiagnostic);
                }
            }

            if (mismatch.Kind.HasFlag(MismatchKind.Name)
                && !IsEitherFieldSuppressed(DiagnosticDescriptors.TransferFieldsNameMismatch.Id, mismatch.Source, mismatch.Target))
            {
                hasNameMismatch = true;
                if (mismatch.FieldId < minNameId)
                    minNameId = mismatch.FieldId;

                if (reportAtFieldLevel)
                {
                    ReportFieldNameMismatch(compilation, getLocation(mismatch.Source), mismatch.Source, mismatch.Target, mismatch.FieldId, sourceDisplay, targetDisplay, reportDiagnostic);
                    ReportFieldNameMismatch(compilation, getLocation(mismatch.Target), mismatch.Source, mismatch.Target, mismatch.FieldId,
                        swapDisplayForTarget ? targetDisplay : sourceDisplay,
                        swapDisplayForTarget ? sourceDisplay : targetDisplay,
                        reportDiagnostic);
                }
            }
        }

        return new MismatchResult(hasTypeMismatch, hasNameMismatch, minTypeId, minNameId);
    }

    private static void ReportFieldTypeMismatch(
        Compilation compilation,
        Location? location,
        IFieldSymbol sourceField,
        IFieldSymbol targetField,
        int fieldId,
        string sourceDisplay,
        string targetDisplay,
        Action<Diagnostic> reportDiagnostic)
    {
        if (location is null || !location.IsInSource || !IsLocationInCompilation(location, compilation))
            return;

        reportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.TransferFieldsTypeMismatch,
            location,
            fieldId,
            sourceDisplay,
            targetDisplay,
            GetToDisplayStringSafe(sourceField),
            GetToDisplayStringSafe(targetField),
            sourceField.Name.QuoteIdentifierIfNeededWithReflection(),
            targetField.Name.QuoteIdentifierIfNeededWithReflection()));
    }

    private static void ReportFieldNameMismatch(
        Compilation compilation,
        Location? location,
        IFieldSymbol sourceField,
        IFieldSymbol targetField,
        int fieldId,
        string sourceDisplay,
        string targetDisplay,
        Action<Diagnostic> reportDiagnostic)
    {
        if (location is null || !location.IsInSource || !IsLocationInCompilation(location, compilation))
            return;

        reportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.TransferFieldsNameMismatch,
            location,
            fieldId,
            sourceDisplay,
            targetDisplay,
            sourceField.Name.QuoteIdentifierIfNeededWithReflection(),
            targetField.Name.QuoteIdentifierIfNeededWithReflection()));
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