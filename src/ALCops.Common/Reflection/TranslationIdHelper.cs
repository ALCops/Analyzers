#if !NETSTANDARD2_1
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Translation;

namespace ALCops.Common.Reflection;

/// <summary>
/// COMPAT: Computes the XLIFF trans-unit ID for a translatable symbol across SDK versions.
///
/// In AL SDK 18.0.38.52553 the public <c>LanguageFileUtilities</c> translation-ID methods
/// (<c>GetLanguageSymbolId</c> / <c>GetLabelTextConstLanguageSymbolId</c>) gained an optional
/// <c>bool useNamespaces = false</c> parameter. C# bakes optional-parameter defaults into the call
/// site, so a direct call compiled against an older SDK (2-param) throws
/// <see cref="MissingMethodException"/> when the analyzer runs against the newer SDK (3-param).
/// All ID computation therefore goes through reflection so no arity is baked into the call site.
///
/// <list type="bullet">
/// <item>New SDK: use internal <c>GetTranslationFileId(...)</c> + public
/// <c>UseTranslationsWithNamespaces(ISymbol)</c> so the computed ID matches the compiler-generated
/// trans-unit ID (including namespace-aware IDs).</item>
/// <item>Old SDK: fall back to the public 2-param <c>GetLanguageSymbolId</c> /
/// <c>GetLabelTextConstLanguageSymbolId</c>.</item>
/// </list>
///
/// This helper lives in ALCops.Common so all SDK-version-compat reflection is centralized; future
/// SDK bumps only need changes here rather than in the analyzer. On netstandard2.1 the underlying
/// SDK methods do not exist, so this helper is compiled out entirely (callers are expected to
/// provide an inert stub there).
/// </summary>
public static class TranslationIdHelper
{
    // GetTranslationFileId(string name, SymbolKind kind, Symbol containingSymbol, bool isMissingCaption,
    //                      IRootTypeSymbol? rootSymbolOverride, bool useNamespaces) — internal, new SDK only.
    private static readonly Lazy<MethodInfo?> _getTranslationFileIdMethod = new(() =>
        typeof(LanguageFileUtilities)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "GetTranslationFileId" && m.GetParameters().Length == 6));

    // UseTranslationsWithNamespaces(ISymbol) — public, new SDK only.
    private static readonly Lazy<MethodInfo?> _useTranslationsWithNamespacesMethod = new(() =>
        typeof(LanguageFileUtilities).GetMethod(
            "UseTranslationsWithNamespaces",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(ISymbol)],
            null));

    // Old-SDK fallbacks: public 2-param methods, present only on SDKs without the namespace feature.
    private static readonly Lazy<MethodInfo?> _getLanguageSymbolIdMethod = new(() =>
        typeof(LanguageFileUtilities).GetMethod(
            "GetLanguageSymbolId",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(ISymbol), typeof(IRootTypeSymbol)],
            null));

    private static readonly Lazy<MethodInfo?> _getLabelTextConstLanguageSymbolIdMethod = new(() =>
        typeof(LanguageFileUtilities).GetMethod(
            "GetLabelTextConstLanguageSymbolId",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(ISymbol), typeof(IRootTypeSymbol)],
            null));

    // Per-symbol-type cache for the private `name` field looked up on source symbol types.
    // Never call reflection lookups on a hot path without caching — see common-library instructions.
    private static readonly ConcurrentDictionary<Type, FieldInfo?> _nameFieldCache = new();

    // Per-symbol lock table used to serialize the temporary `name`-field mutation across concurrent
    // analyzer callbacks touching the same symbol instance. `ConditionalWeakTable` holds the symbol
    // by weak reference so the lock object is collected together with the symbol. Cross-analyzer
    // races are not covered (other analyzers do not know about this lock) but same-helper races on
    // the same symbol are eliminated.
    private static readonly ConditionalWeakTable<ISymbol, object> _symbolLocks = new();

    /// <summary>
    /// Computes the XLIFF trans-unit ID for the given translatable symbol, matching the ID the AL
    /// compiler generates. Returns <see langword="null"/> when no compatible SDK method can be resolved
    /// (in which case callers should skip the symbol rather than report a false positive).
    /// </summary>
    /// <param name="symbol">The translatable symbol (property, label variable, or report label).</param>
    /// <param name="rootSymbol">The translation root symbol override, or <see langword="null"/>.</param>
    /// <param name="isLabelConst">
    /// <see langword="true"/> for label text constants (uses the NamedType symbol kind and the
    /// label-const fallback); <see langword="false"/> for properties and report labels.
    /// </param>
    /// <param name="nameOverride">
    /// Optional canonical name to use in place of <c>symbol.Name</c> when building the ID (e.g.
    /// <c>propertyKind.ToString() == "ToolTip"</c> for a source-cased "Tooltip"). Guarantees the
    /// computed trans-unit ID matches the compiler-generated one regardless of the casing used in
    /// source. The SDK's translation-ID methods hash the property/label name from the live symbol
    /// instance (not from any string parameter they declare), so this helper temporarily rewrites
    /// the symbol's private <c>name</c> field for the duration of the call and restores it in a
    /// <c>finally</c>. No state leaks to subsequent callbacks. Applies to both the new-SDK
    /// (<c>GetTranslationFileId</c>) and old-SDK (<c>GetLanguageSymbolId</c> /
    /// <c>GetLabelTextConstLanguageSymbolId</c>) paths.
    /// </param>
    public static string? ComputeTranslationId(ISymbol symbol, IRootTypeSymbol? rootSymbol, bool isLabelConst, string? nameOverride = null)
    {
        MethodInfo? translationFileId = _getTranslationFileIdMethod.Value;
        if (translationFileId is not null)
        {
            SymbolKind kind = isLabelConst ? EnumProvider.SymbolKind.NamedType : symbol.Kind;
            bool useNamespaces = GetUseTranslationsWithNamespaces(symbol);

            return InvokeWithCanonicalName(symbol, nameOverride,
                () => translationFileId.Invoke(null,
                    [symbol.Name, kind, symbol.ContainingSymbol, false, rootSymbol, useNamespaces]) as string);
        }

        MethodInfo? fallback = isLabelConst
            ? _getLabelTextConstLanguageSymbolIdMethod.Value
            : _getLanguageSymbolIdMethod.Value;

        return InvokeWithCanonicalName(symbol, nameOverride,
            () => fallback?.Invoke(null, [symbol, rootSymbol]) as string);
    }

    /// <summary>
    /// Invokes <paramref name="compute"/> with the target <paramref name="symbol"/>'s private
    /// <c>name</c> field temporarily replaced by <paramref name="nameOverride"/>, restoring the
    /// original value in <c>finally</c>. Concurrent callers of this helper on the same symbol are
    /// serialized through a per-symbol lock so no thread inside this helper observes the mutated
    /// value across the SDK call boundary.
    /// <para>
    /// The SDK's internal <c>GetTranslationFileId</c> hashes the property/label name from the
    /// live symbol instance rather than from the <c>name</c> string parameter it declares (the
    /// parameter feeds only human-readable note segments). There is no public seam to override
    /// the hashed name, so we mutate the private field on <c>SourcePropertySymbol</c> (or the
    /// equivalent source symbol) around the call. All source symbols we hit follow the same
    /// convention of storing the name in a single <c>name</c> field.
    /// </para>
    /// <para>
    /// The per-symbol lock only synchronizes this helper's own callers. Other analyzers reading
    /// <c>symbol.Name</c> concurrently do not participate and could still, in theory, observe the
    /// canonical name during the mutation window. In practice this is limited to the diagnostic
    /// message text (never to computed IDs or analysis decisions).
    /// </para>
    /// </summary>
    private static string? InvokeWithCanonicalName(ISymbol symbol, string? nameOverride, Func<string?> compute)
    {
        if (nameOverride is null || string.Equals(nameOverride, symbol.Name, StringComparison.Ordinal))
            return compute();

        FieldInfo? nameField = _nameFieldCache.GetOrAdd(symbol.GetType(), static t =>
        {
            FieldInfo? field = t.GetField("name", BindingFlags.Instance | BindingFlags.NonPublic);

            return field is not null && field.FieldType == typeof(string) ? field : null;
        });

        if (nameField is null)
            return compute();

        object symbolLock = _symbolLocks.GetValue(symbol, static _ => new object());

        lock (symbolLock)
        {
            string? original = (string?)nameField.GetValue(symbol);
            nameField.SetValue(symbol, nameOverride);

            try
            {
                return compute();
            }
            finally
            {
                nameField.SetValue(symbol, original);
            }
        }
    }

    private static bool GetUseTranslationsWithNamespaces(ISymbol symbol)
    {
        MethodInfo? method = _useTranslationsWithNamespacesMethod.Value;
        if (method is null)
            return false;

        return method.Invoke(null, [symbol]) is true;
    }
}
#endif
