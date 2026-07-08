#if !NETSTANDARD2_1
using System.Reflection;
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
/// SDK methods do not exist, so this helper is compiled out entirely (the LC0091 analyzer is an inert
/// stub there).
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
    public static string? ComputeTranslationId(ISymbol symbol, IRootTypeSymbol? rootSymbol, bool isLabelConst)
    {
        MethodInfo? translationFileId = _getTranslationFileIdMethod.Value;
        if (translationFileId is not null)
        {
            SymbolKind kind = isLabelConst ? EnumProvider.SymbolKind.NamedType : symbol.Kind;
            bool useNamespaces = GetUseTranslationsWithNamespaces(symbol);

            return translationFileId.Invoke(null,
                [symbol.Name, kind, symbol.ContainingSymbol, false, rootSymbol, useNamespaces]) as string;
        }

        MethodInfo? fallback = isLabelConst
            ? _getLabelTextConstLanguageSymbolIdMethod.Value
            : _getLanguageSymbolIdMethod.Value;

        return fallback?.Invoke(null, [symbol, rootSymbol]) as string;
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
