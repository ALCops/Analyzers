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
    /// Canonical name to hash in place of <c>symbol.Name</c> for the leaf trans-unit segment
    /// (e.g. <c>"ToolTip"</c> for a source-cased <c>"Tooltip"</c>). New-SDK path forwards it as
    /// the <c>name</c> string parameter to <c>GetTranslationFileId</c> (no mutation). Old-SDK
    /// fallback rewrites the symbol's private <c>name</c> field for the duration of the call and
    /// restores it in <c>finally</c> (see <see cref="InvokeWithCanonicalName"/>). Pass
    /// <see langword="null"/> to hash <c>symbol.Name</c> unchanged.
    /// </param>
    public static string? ComputeTranslationId(ISymbol symbol, IRootTypeSymbol? rootSymbol, bool isLabelConst, string? nameOverride = null)
    {
        MethodInfo? translationFileId = _getTranslationFileIdMethod.Value;
        if (translationFileId is not null)
        {
            SymbolKind kind = isLabelConst ? EnumProvider.SymbolKind.NamedType : symbol.Kind;
            bool useNamespaces = GetUseTranslationsWithNamespaces(symbol);
            string effectiveName = nameOverride ?? symbol.Name;

            return translationFileId.Invoke(null,
                [effectiveName, kind, symbol.ContainingSymbol, false, rootSymbol, useNamespaces]) as string;
        }

        MethodInfo? fallback = isLabelConst
            ? _getLabelTextConstLanguageSymbolIdMethod.Value
            : _getLanguageSymbolIdMethod.Value;

        return InvokeWithCanonicalName(symbol, nameOverride,
            () => fallback?.Invoke(null, [symbol, rootSymbol]) as string);
    }

    /// <summary>
    /// Old-SDK fallback helper: invokes <paramref name="compute"/> with the target symbol's
    /// private <c>name</c> field temporarily replaced by <paramref name="nameOverride"/> and
    /// restored in <c>finally</c>. Short-circuits to <paramref name="compute"/> when the override
    /// is <see langword="null"/> or already matches <c>symbol.Name</c>.
    /// </summary>
    /// <remarks>
    /// Needed because the public 2-param <c>GetLanguageSymbolId(ISymbol, IRootTypeSymbol?)</c> and
    /// <c>GetLabelTextConstLanguageSymbolId(ISymbol, IRootTypeSymbol?)</c> overloads read
    /// <c>symbol.Name</c> internally and expose no name parameter. The new-SDK
    /// <c>GetTranslationFileId</c> path takes the name as a string parameter and bypasses this
    /// helper entirely. The mutation targets the private <c>name</c> field on
    /// <c>SourcePropertySymbol</c> (or equivalent source symbol); all source symbols we hit store
    /// the name in that single field.
    /// <para>
    /// Mutations on the same symbol are serialized via <see cref="_symbolLocks"/> (a
    /// <see cref="ConditionalWeakTable{TKey, TValue}"/> so lock objects are collected with the
    /// symbol). Other analyzers reading <c>symbol.Name</c> concurrently do not participate and may
    /// briefly observe the canonical name during the SDK call; scope is limited to the fallback
    /// path on SDKs predating <c>GetTranslationFileId</c>.
    /// </para>
    /// </remarks>
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
