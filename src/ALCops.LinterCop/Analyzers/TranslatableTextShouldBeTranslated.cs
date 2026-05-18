using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

#if !NETSTANDARD2_1
using System.Xml.Linq;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis.Emit;
using Microsoft.Dynamics.Nav.CodeAnalysis.Packaging;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Translation;
#endif

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class TranslatableTextShouldBeTranslated : DiagnosticAnalyzer
{
#if NETSTANDARD2_1
    // ExtensionObjectFoldingUtilities, LanguageFileUtilities.GetLanguageSymbolId(ISymbol, IRootTypeSymbol?),
    // and LanguageFileUtilities.GetLabelTextConstLanguageSymbolId do not exist in the netstandard2.1 SDK.
    // The rule is only active on net8.0+.
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray<DiagnosticDescriptor>.Empty;

    public override void Initialize(AnalysisContext context) { }
#else
    private static readonly XNamespace XliffNamespace = "urn:oasis:names:tc:xliff:document:1.2";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.TranslatableTextShouldBeTranslated);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        Compilation compilation = context.Compilation;
        IFileSystem? fileSystem = compilation.FileSystem;
        if (fileSystem is null)
            return;

        NavAppManifest? manifest;
        try
        {
            manifest = ManifestHelper.GetManifest(compilation);
        }
        catch (FileNotFoundException)
        {
            manifest = null;
        }

        if (manifest is not null && !manifest.CompilerFeatures.ShouldGenerateTranslationFile())
            return;

        ALCopsSettings settings = ALCopsSettingsProvider.GetSettings(fileSystem);

        string appName = manifest?.AppName ?? string.Empty;
        TranslationIndex? translationIndex = BuildTranslationIndex(fileSystem, appName, settings.LanguagesToTranslate);
        if (translationIndex is null || translationIndex.AvailableLanguages.Count == 0)
            return;

        context.RegisterSymbolAction(
            ctx => AnalyzeSymbol(ctx, translationIndex),
            EnumProvider.SymbolKind.Table,
            EnumProvider.SymbolKind.TableExtension,
            EnumProvider.SymbolKind.Page,
            EnumProvider.SymbolKind.PageExtension,
            EnumProvider.SymbolKind.Report,
            EnumProvider.SymbolKind.XmlPort,
            EnumProvider.SymbolKind.Enum,
            EnumProvider.SymbolKind.EnumValue,
            EnumProvider.SymbolKind.Query,
            EnumProvider.SymbolKind.Profile,
            EnumProvider.SymbolKind.PermissionSet,
            EnumProvider.SymbolKind.Field,
            EnumProvider.SymbolKind.GlobalVariable,
            EnumProvider.SymbolKind.LocalVariable,
            EnumProvider.SymbolKind.ReportLabel,
            EnumProvider.SymbolKind.RequestPage,
            EnumProvider.SymbolKind.RequestPageExtension
        );
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext ctx, TranslationIndex translationIndex)
    {
        if (ctx.IsObsolete())
            return;

        ISymbol symbol = ctx.Symbol;
        SymbolKind kind = symbol.Kind;

        if (kind == EnumProvider.SymbolKind.LocalVariable || kind == EnumProvider.SymbolKind.GlobalVariable)
        {
            AnalyzeLabelVariable(ctx, symbol, translationIndex);
            return;
        }

        if (kind == EnumProvider.SymbolKind.ReportLabel)
        {
            AnalyzeReportLabel(ctx, symbol, translationIndex);
            return;
        }

        if (kind == EnumProvider.SymbolKind.Field)
        {
            ReportTranslatableProperty(ctx, symbol, EnumProvider.PropertyKind.Caption, translationIndex);
            ReportTranslatableProperty(ctx, symbol, EnumProvider.PropertyKind.ToolTip, translationIndex);
            return;
        }

        if (kind == EnumProvider.SymbolKind.Page
            || kind == EnumProvider.SymbolKind.PageExtension
            || kind == EnumProvider.SymbolKind.RequestPage
            || kind == EnumProvider.SymbolKind.RequestPageExtension
            || kind == EnumProvider.SymbolKind.Query)
        {
            AnalyzePageLikeSymbol(ctx, symbol, translationIndex);
            return;
        }

        // Table, TableExtension, XmlPort, Enum, EnumValue, Report, Profile, PermissionSet
        ReportTranslatableProperty(ctx, symbol, EnumProvider.PropertyKind.Caption, translationIndex);
    }

    private static void AnalyzeLabelVariable(SymbolAnalysisContext ctx, ISymbol symbol, TranslationIndex translationIndex)
    {
        if (symbol is not IVariableSymbol variable)
            return;

        if (variable.Type.NavTypeKind != EnumProvider.NavTypeKind.Label)
            return;

        if (variable.Type is ILabelTypeSymbol { Locked: true })
            return;

        IRootTypeSymbol? rootSymbol = ExtensionObjectFoldingUtilities.GetTranslationRootSymbol(symbol);
        string translationId = LanguageFileUtilities.GetLabelTextConstLanguageSymbolId(symbol, rootSymbol);

        ReportMissingTranslation(ctx, symbol, translationId, translationIndex);
    }

    private static void AnalyzeReportLabel(SymbolAnalysisContext ctx, ISymbol symbol, TranslationIndex translationIndex)
    {
        if (symbol.ContainingSymbol?.IsObsolete() == true)
            return;

        if (IsPropertyLocked(symbol))
            return;

        IRootTypeSymbol? rootSymbol = ExtensionObjectFoldingUtilities.GetTranslationRootSymbol(symbol);
        string translationId = LanguageFileUtilities.GetLanguageSymbolId(symbol, rootSymbol);

        ReportMissingTranslation(ctx, symbol, translationId, translationIndex);
    }

    private static void AnalyzePageLikeSymbol(SymbolAnalysisContext ctx, ISymbol symbol, TranslationIndex translationIndex)
    {
        ReportTranslatableProperty(ctx, symbol, EnumProvider.PropertyKind.Caption, translationIndex);

        IEnumerable<IControlSymbol>? controls = GetFlattenedControls(symbol);
        if (controls is not null)
        {
            foreach (IControlSymbol control in controls)
            {
                if (control.IsObsolete())
                    continue;

                ReportTranslatableProperty(ctx, control, EnumProvider.PropertyKind.Caption, translationIndex);
                ReportTranslatableProperty(ctx, control, EnumProvider.PropertyKind.ToolTip, translationIndex);
                ReportTranslatableProperty(ctx, control, EnumProvider.PropertyKind.OptionCaption, translationIndex);
            }
        }

        IEnumerable<IActionSymbol>? actions = GetFlattenedActions(symbol);
        if (actions is not null)
        {
            foreach (IActionSymbol action in actions)
            {
                if (action.IsObsolete())
                    continue;

                ReportTranslatableProperty(ctx, action, EnumProvider.PropertyKind.Caption, translationIndex);
                ReportTranslatableProperty(ctx, action, EnumProvider.PropertyKind.ToolTip, translationIndex);
            }
        }
    }

    private static void ReportTranslatableProperty(SymbolAnalysisContext ctx, ISymbol symbol, PropertyKind propertyKind, TranslationIndex translationIndex)
    {
        IPropertySymbol? property = symbol.GetProperty(propertyKind);
        if (property is null)
            return;

        if (property.ContainingSymbol?.IsObsolete() == true)
            return;

        if (IsPropertyLocked(property))
            return;

        IRootTypeSymbol? rootSymbol = ExtensionObjectFoldingUtilities.GetTranslationRootSymbol(property);
        string translationId = LanguageFileUtilities.GetLanguageSymbolId(property, rootSymbol);

        ReportMissingTranslation(ctx, property, translationId, translationIndex);
    }

    private static void ReportMissingTranslation(SymbolAnalysisContext ctx, ISymbol symbol, string translationId, TranslationIndex translationIndex)
    {
        HashSet<string> missingLanguages = translationIndex.GetMissingLanguages(translationId);
        if (missingLanguages.Count == 0)
            return;

        string languages = string.Join(", ", missingLanguages.OrderBy(lang => lang, StringComparer.Ordinal));

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.TranslatableTextShouldBeTranslated,
            symbol.GetLocation(),
            symbol.Name,
            languages));
    }

    private static bool IsPropertyLocked(ISymbol symbol)
    {
        SyntaxReference? syntaxReference = symbol switch
        {
            IPropertySymbol => symbol.DeclaringSyntaxReference,
            IReportLabelSymbol => symbol.DeclaringSyntaxReference,
            _ => null
        };

        if (syntaxReference is null)
            return true; // can't determine, err on the side of not reporting

        SyntaxNode? syntaxNode = syntaxReference.GetSyntax();
        if (syntaxNode is null)
            return true;

        SyntaxNode? subPropertyNode = syntaxNode.DescendantNodes()
            .FirstOrDefault(n => n.Kind == EnumProvider.SyntaxKind.CommaSeparatedIdentifierEqualsLiteralList);

        if (subPropertyNode is null)
            return false;

        bool? locked = subPropertyNode.GetBooleanPropertyValue(IdentifierProperty.Locked);
        return locked == true;
    }

    #region XLIFF Parsing

    private static TranslationIndex? BuildTranslationIndex(IFileSystem fileSystem, string appName, string[]? languagesToTranslate)
    {
        HashSet<string>? languageFilter = languagesToTranslate is { Length: > 0 }
            ? new HashSet<string>(languagesToTranslate, StringComparer.OrdinalIgnoreCase)
            : null;

        IEnumerable<string> xliffFiles;
        try
        {
            xliffFiles = LanguageFileUtilities.GetXliffLanguageFiles(fileSystem, appName);
        }
        catch (DirectoryNotFoundException)
        {
            if (languageFilter is { Count: > 0 })
                return new TranslationIndex(
                    new HashSet<string>(languageFilter, StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, HashSet<string>>(StringComparer.Ordinal));

            return null;
        }

        HashSet<string> availableLanguages = languageFilter is not null
            ? new HashSet<string>(languageFilter, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<(string Language, XDocument Document)> parsedFiles = new();

        foreach (string xliffPath in xliffFiles)
        {
            XDocument doc;
            try
            {
                using Stream stream = fileSystem.OpenRead(xliffPath);
                doc = XDocument.Load(stream);
            }
            catch
            {
                continue;
            }

            string? language = doc.Descendants(XliffNamespace + "file")
                .FirstOrDefault()
                ?.Attribute("target-language")
                ?.Value;

            if (string.IsNullOrEmpty(language))
                continue;

            if (languageFilter is not null && !languageFilter.Contains(language))
                continue;

            availableLanguages.Add(language);
            parsedFiles.Add((language, doc));
        }

        if (availableLanguages.Count == 0)
            return null;

        // Build the translation index: for each trans-unit ID, track which languages are missing
        Dictionary<string, HashSet<string>> index = new(StringComparer.Ordinal);

        foreach ((string language, XDocument doc) in parsedFiles)
        {
            IEnumerable<XElement>? transUnits = doc.Descendants(XliffNamespace + "trans-unit");

            foreach (XElement transUnit in transUnits)
            {
                string? id = transUnit.Attribute("id")?.Value;
                if (string.IsNullOrEmpty(id))
                    continue;

                if (!index.TryGetValue(id, out HashSet<string>? missingForId))
                {
                    missingForId = new HashSet<string>(availableLanguages, StringComparer.OrdinalIgnoreCase);
                    index[id] = missingForId;
                }

                XElement? targetElement = transUnit.Element(XliffNamespace + "target");
                bool isMissing = targetElement is null
                    || string.IsNullOrWhiteSpace(targetElement.Value)
                    || targetElement.Attribute("state")?.Value == "needs-translation";

                if (!isMissing)
                    missingForId.Remove(language);
            }
        }

        return new TranslationIndex(availableLanguages, index);
    }

    #endregion

    #region Helpers

    private static IEnumerable<IControlSymbol>? GetFlattenedControls(ISymbol symbol) =>
        symbol switch
        {
            IPageBaseTypeSymbol page => page.FlattenedControls,
            IPageExtensionBaseTypeSymbol pageExtension => pageExtension.AddedControlsFlattened,
            IRequestPageExtensionTypeSymbol requestPageExtension => requestPageExtension.AddedControlsFlattened,
            _ => null
        };

    private static IEnumerable<IActionSymbol>? GetFlattenedActions(ISymbol symbol) =>
        symbol switch
        {
            IPageBaseTypeSymbol page => page.FlattenedActions,
            IPageExtensionBaseTypeSymbol pageExtension => pageExtension.AddedActionsFlattened,
            _ => null
        };

    #endregion

    #region TranslationIndex

    private sealed class TranslationIndex(HashSet<string> availableLanguages, Dictionary<string, HashSet<string>> index)
    {
        public HashSet<string> AvailableLanguages { get; } = availableLanguages;
        private Dictionary<string, HashSet<string>> Index { get; } = index;

        public HashSet<string> GetMissingLanguages(string translationId)
        {
            if (!Index.TryGetValue(translationId, out HashSet<string>? missingLanguages))
                return new HashSet<string>(AvailableLanguages, StringComparer.OrdinalIgnoreCase);

            return missingLanguages.Intersect(AvailableLanguages).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }

    #endregion
#endif
}
