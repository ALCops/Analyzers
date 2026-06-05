using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

using NamingPatternSetting = ALCops.Common.Settings.NamingPattern;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class NamingPattern : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.NamingPattern);

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCompilationStartAction(CompilationStart);

    private void CompilationStart(CompilationStartAnalysisContext ctx)
    {
        var settings = ALCopsSettingsProvider.GetSettings(ctx.Compilation.FileSystem);

        List<string>? affixes = null;
        try
        {
            affixes = GetAffixes(ctx.Compilation);
        }
        catch
        {
            // AppSourceCop configuration may not be available in test contexts
        }

        var config = new NamingPatternConfig(settings.NamingPatterns);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeMethod(symbolCtx, config),
            EnumProvider.SymbolKind.Method);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeVariable(symbolCtx, config),
            EnumProvider.SymbolKind.LocalVariable,
            EnumProvider.SymbolKind.GlobalVariable);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeObject(symbolCtx, config, affixes),
            EnumProvider.SymbolKind.Table,
            EnumProvider.SymbolKind.Page,
            EnumProvider.SymbolKind.Codeunit,
            EnumProvider.SymbolKind.Report,
            EnumProvider.SymbolKind.Query,
            EnumProvider.SymbolKind.XmlPort,
            EnumProvider.SymbolKind.Enum,
            EnumProvider.SymbolKind.Interface,
            EnumProvider.SymbolKind.PermissionSet);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeField(symbolCtx, config),
            EnumProvider.SymbolKind.Field);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeEnumValue(symbolCtx, config),
            EnumProvider.SymbolKind.EnumValue);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeAction(symbolCtx, config),
            EnumProvider.SymbolKind.Action);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeControl(symbolCtx, config),
            EnumProvider.SymbolKind.Control);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext ctx, NamingPatternConfig config)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IMethodSymbol method)
            return;

        // Skip triggers (platform-defined names)
        if (method.MethodKind != EnumProvider.MethodKind.Method)
            return;

        // Skip interface-implementing methods (can't change name)
        if (method.MethodImplementsInterfaceMethod())
            return;

        // Classify the method to determine which naming target applies
        var target = ClassifyMethod(method);
        CheckName(ctx, method.Name, target, config, GetKindDisplayName(target));

        // Skip parameter and return value checks for event subscribers.
        // Subscriber parameters must match the publisher signature (AL0828);
        // platform trigger params (xRec, BelowxRec, RunTrigger, etc.) can't be renamed.
        if (target == NamingTarget.EventSubscriber)
            return;

        // Check parameters
        foreach (var parameter in method.Parameters)
        {
            if (string.IsNullOrEmpty(parameter.Name))
                continue;

            CheckNameForSymbol(ctx, parameter, parameter.Name, NamingTarget.Parameter, config, "Parameter");
        }

        // Check return value
        if (method.ReturnValueSymbol is { } returnValue &&
            !string.IsNullOrEmpty(returnValue.Name))
        {
            CheckNameForSymbol(ctx, returnValue, returnValue.Name, NamingTarget.ReturnValue, config, "Return value");
        }
    }

    private static void AnalyzeVariable(SymbolAnalysisContext ctx, NamingPatternConfig config)
    {
        if (ctx.IsObsolete())
            return;

        CheckName(ctx, ctx.Symbol.Name, NamingTarget.Variable, config, "Variable");
    }

    private static void AnalyzeObject(SymbolAnalysisContext ctx, NamingPatternConfig config,
        List<string>? affixes)
    {
        if (ctx.IsObsolete())
            return;

        var name = StripAffixes(ctx.Symbol.Name, affixes);
        CheckName(ctx, name, NamingTarget.Object, config, "Object");
    }

    private static void AnalyzeField(SymbolAnalysisContext ctx, NamingPatternConfig config)
    {
        if (ctx.IsObsolete())
            return;

        CheckName(ctx, ctx.Symbol.Name, NamingTarget.Field, config, "Field");
    }

    private static void AnalyzeEnumValue(SymbolAnalysisContext ctx, NamingPatternConfig config)
    {
        if (ctx.IsObsolete())
            return;

        CheckName(ctx, ctx.Symbol.Name, NamingTarget.EnumValue, config, "Enum value");
    }

    private static void AnalyzeAction(SymbolAnalysisContext ctx, NamingPatternConfig config)
    {
        if (ctx.IsObsolete())
            return;

        CheckName(ctx, ctx.Symbol.Name, NamingTarget.Action, config, "Action");
    }

    private static void AnalyzeControl(SymbolAnalysisContext ctx, NamingPatternConfig config)
    {
        if (ctx.IsObsolete())
            return;

        // Skip controls on API objects (pages with PageType=API, queries with QueryType=API).
        // API controls require camelCase per AA0102, which conflicts with the default PascalCase pattern.
        if (IsInApiObject(ctx.Symbol))
            return;

        CheckName(ctx, ctx.Symbol.Name, NamingTarget.Control, config, "Control");
    }

    private static bool IsInApiObject(ISymbol symbol)
    {
        var containingSymbol = symbol.ContainingSymbol;
        while (containingSymbol is not null)
        {
            if (containingSymbol is IPageTypeSymbol pageType)
                return pageType.PageType == EnumProvider.PageTypeKind.API;

            if (containingSymbol is IQueryTypeSymbol queryType)
                return queryType.QueryType == EnumProvider.QueryTypeKind.API;

            containingSymbol = containingSymbol.ContainingSymbol;
        }
        return false;
    }

    private static void CheckName(SymbolAnalysisContext ctx, string name, NamingTarget target,
        NamingPatternConfig config, string kindDisplayName)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        // Strip & keyboard accelerator markers for action/control names.
        // The & prefix designates keyboard shortcuts (e.g., "&Line" renders
        // as underlined "L" for Alt+L). Only stripped for UI element names.
        var nameForCheck = target is NamingTarget.Action or NamingTarget.Control
            ? name.Replace("&", "")
            : name;

        if (string.IsNullOrWhiteSpace(nameForCheck))
            return;

        var resolved = config.GetPatterns(target);

        if (resolved.AllowRegex is not null)
        {
            if (!TryIsMatch(resolved.AllowRegex, nameForCheck))
            {
                var message = BuildMessage(
                    nameForCheck, resolved.AllowPatternString, resolved.AllowDescription, isAllow: true);
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NamingPattern,
                    ctx.Symbol.GetLocation(),
                    kindDisplayName,
                    name,
                    message));
            }
        }

        if (resolved.DisallowRegex is not null)
        {
            if (TryIsMatch(resolved.DisallowRegex, nameForCheck))
            {
                var message = BuildMessage(
                    nameForCheck, resolved.DisallowPatternString, resolved.DisallowDescription, isAllow: false);
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NamingPattern,
                    ctx.Symbol.GetLocation(),
                    kindDisplayName,
                    name,
                    message));
            }
        }
    }

    private static void CheckNameForSymbol(SymbolAnalysisContext ctx, ISymbol symbol,
        string name, NamingTarget target, NamingPatternConfig config, string kindDisplayName)
    {
        if (string.IsNullOrEmpty(name))
            return;

        var resolved = config.GetPatterns(target);

        if (resolved.AllowRegex is not null)
        {
            if (!TryIsMatch(resolved.AllowRegex, name))
            {
                var message = BuildMessage(
                    name, resolved.AllowPatternString, resolved.AllowDescription, isAllow: true);
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NamingPattern,
                    symbol.GetLocation(),
                    kindDisplayName,
                    name,
                    message));
            }
        }

        if (resolved.DisallowRegex is not null)
        {
            if (TryIsMatch(resolved.DisallowRegex, name))
            {
                var message = BuildMessage(
                    name, resolved.DisallowPatternString, resolved.DisallowDescription, isAllow: false);
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NamingPattern,
                    symbol.GetLocation(),
                    kindDisplayName,
                    name,
                    message));
            }
        }
    }

    private static string BuildMessage(string name, string? patternString, string? description, bool isAllow)
    {
        var suggestion = TryGenerateSuggestion(name, patternString, isAllow);
        var sugSuffix = suggestion is not null ? $". Consider: \"{suggestion}\"" : "";

        // Tier 1: User or built-in description
        if (!string.IsNullOrEmpty(description))
            return $"{description}{sugSuffix}";

        // Tier 3: Mini regex explainer
        var explained = RegexExplainer.TryExplain(patternString, isAllow);
        if (explained is not null)
            return $"{explained}{sugSuffix}";

        // Tier 4: Raw regex fallback
        var verb = isAllow ? "must match" : "must not match";
        return $"{verb} pattern \"{patternString}\"{sugSuffix}";
    }

    private static string? TryGenerateSuggestion(string name, string? patternString, bool isAllow)
    {
        if (string.IsNullOrEmpty(patternString) || string.IsNullOrEmpty(name))
            return null;

        if (isAllow)
        {
            // ^[A-Z] or patterns with [A-Z] start - capitalize first character
            if ((patternString == @"^[A-Z]"
                || patternString == @"^(?:[A-Za-z]$|[A-Z])"
                || patternString == @"^(?:[A-Za-z]$|[A-Z]|_[A-Z])"
                || patternString == @"^(?:[A-Za-z]$|[A-Z]|x[A-Z])"
                || patternString == @"^(?:[A-Za-z]$|[A-Z]|_[A-Z]|x[A-Z])")
                && name.Length > 1 && char.IsLower(name[0]))
                return char.ToUpperInvariant(name[0]) + name.Substring(1);

            // ^[a-z] - lowercase first character
            if (patternString == @"^[a-z]" && name.Length > 0 && char.IsUpper(name[0]))
                return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
        else
        {
            // [%&!?] - remove disallowed characters
            if (patternString == @"[%&!?]")
            {
                var cleaned = Regex.Replace(name, @"[%&!?]", "");
                return cleaned != name ? cleaned : null;
            }
        }

        return null;
    }

    private static bool TryIsMatch(Regex pattern, string input)
    {
        try
        {
            return pattern.IsMatch(input);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static NamingTarget ClassifyMethod(IMethodSymbol method)
    {
        foreach (var attribute in method.Attributes)
        {
            if (attribute.AttributeKind == EnumProvider.AttributeKind.EventSubscriber)
                return NamingTarget.EventSubscriber;

            if (attribute.AttributeKind == EnumProvider.AttributeKind.IntegrationEvent ||
                attribute.AttributeKind == EnumProvider.AttributeKind.BusinessEvent)
                return NamingTarget.EventDeclaration;
        }

        if (method.IsLocal)
            return NamingTarget.LocalProcedure;

        return NamingTarget.GlobalProcedure;
    }

    private static string GetKindDisplayName(NamingTarget target) => target switch
    {
        NamingTarget.Procedure => "Procedure",
        NamingTarget.LocalProcedure => "Procedure",
        NamingTarget.GlobalProcedure => "Procedure",
        NamingTarget.EventSubscriber => "Event subscriber",
        NamingTarget.EventDeclaration => "Event declaration",
        _ => "Procedure"
    };

    private static string StripAffixes(string name, List<string>? affixes)
    {
        if (affixes is null || affixes.Count == 0)
            return name;

        foreach (var affix in affixes)
        {
            if (name.StartsWith(affix, SemanticFacts.NameEqualityComparison) &&
                name.Length > affix.Length)
            {
                return name.Substring(affix.Length).TrimStart();
            }

            if (name.EndsWith(affix, SemanticFacts.NameEqualityComparison) &&
                name.Length > affix.Length)
            {
                return name.Substring(0, name.Length - affix.Length).TrimEnd();
            }
        }

        return name;
    }

    private static List<string>? GetAffixes(Compilation compilation)
    {
        AppSourceCopConfiguration? copConfiguration =
            AppSourceCopConfigurationProvider.GetAppSourceCopConfiguration(compilation);

        if (copConfiguration is null)
            return null;

        var affixes = new List<string>();
        if (!string.IsNullOrEmpty(copConfiguration.MandatoryPrefix) &&
            !affixes.Contains(copConfiguration.MandatoryPrefix, StringComparer.OrdinalIgnoreCase))
            affixes.Add(copConfiguration.MandatoryPrefix);

        if (copConfiguration.MandatoryAffixes is not null)
        {
            foreach (string mandatoryAffix in copConfiguration.MandatoryAffixes)
            {
                if (!string.IsNullOrEmpty(mandatoryAffix) &&
                    !affixes.Contains(mandatoryAffix, StringComparer.OrdinalIgnoreCase))
                    affixes.Add(mandatoryAffix);
            }
        }

        return affixes.Count > 0 ? affixes : null;
    }

    internal enum NamingTarget
    {
        Procedure,
        LocalProcedure,
        GlobalProcedure,
        EventSubscriber,
        EventDeclaration,
        Variable,
        Parameter,
        ReturnValue,
        Object,
        Field,
        Action,
        EnumValue,
        Control
    }

    internal sealed class NamingPatternConfig
    {
        private static readonly Dictionary<NamingTarget, (string? Allow, string? Disallow, string? AllowDesc, string? DisallowDesc)> BuiltInDefaults = new()
        {
            [NamingTarget.Procedure] = (@"^[A-Z]", null, "should start with an uppercase letter", null),
            [NamingTarget.Variable] = (@"^(?:[A-Za-z]$|[A-Z]|_[A-Z]|x[A-Z])", @"[%&!?]", "should start with an uppercase letter, underscore followed by uppercase, or x followed by uppercase for xRec pattern (single-letter names are exempt)", "should not contain special characters (%, &, !, ?)"),
            [NamingTarget.Parameter] = (@"^(?:[A-Za-z]$|[A-Z]|_[A-Z]|x[A-Z])", null, "should start with an uppercase letter, underscore followed by uppercase, or x followed by uppercase for xRec pattern (single-letter names are exempt)", null),
            [NamingTarget.ReturnValue] = (@"^[A-Z]", null, "should start with an uppercase letter", null),
            [NamingTarget.Object] = (@"^[A-Z]", null, "should start with an uppercase letter", null),
            [NamingTarget.Field] = (@"^[A-Za-z]", @"[%&!?]", "should start with a letter", "should not contain special characters (%, &, !, ?)"),
            [NamingTarget.Action] = (@"^[A-Z]", null, "should start with an uppercase letter", null),
            [NamingTarget.Control] = (@"^[A-Z]", null, "should start with an uppercase letter", null),
        };

        private static readonly Dictionary<NamingTarget, NamingTarget> InheritanceMap = new()
        {
            [NamingTarget.LocalProcedure] = NamingTarget.Procedure,
            [NamingTarget.GlobalProcedure] = NamingTarget.Procedure,
            [NamingTarget.EventSubscriber] = NamingTarget.Procedure,
            [NamingTarget.EventDeclaration] = NamingTarget.Procedure,
        };

        private readonly Dictionary<NamingTarget, ResolvedPatterns> _resolvedPatterns;

        public NamingPatternConfig(Dictionary<string, NamingPatternSetting>? userOverrides)
        {
            _resolvedPatterns = new Dictionary<NamingTarget, ResolvedPatterns>();

            foreach (NamingTarget target in System.Enum.GetValues(typeof(NamingTarget)))
            {
                var resolved = ResolvePatternStrings(target, userOverrides);
                _resolvedPatterns[target] = new ResolvedPatterns(
                    CompilePattern(resolved.Allow),
                    CompilePattern(resolved.Disallow),
                    resolved.Allow,
                    resolved.Disallow,
                    resolved.AllowDesc,
                    resolved.DisallowDesc);
            }
        }

        public ResolvedPatterns GetPatterns(NamingTarget target) =>
            _resolvedPatterns.TryGetValue(target, out var patterns) ? patterns : ResolvedPatterns.Empty;

        private static (string? Allow, string? Disallow, string? AllowDesc, string? DisallowDesc) ResolvePatternStrings(
            NamingTarget target, Dictionary<string, NamingPatternSetting>? userOverrides)
        {
            // Check if user has explicit override for this target
            if (userOverrides is not null && TryGetUserOverride(userOverrides, target, out var userSetting))
            {
                return (
                    !string.IsNullOrEmpty(userSetting.AllowPattern) ? userSetting.AllowPattern : null,
                    !string.IsNullOrEmpty(userSetting.DisallowPattern) ? userSetting.DisallowPattern : null,
                    !string.IsNullOrEmpty(userSetting.AllowDescription) ? userSetting.AllowDescription : null,
                    !string.IsNullOrEmpty(userSetting.DisallowDescription) ? userSetting.DisallowDescription : null);
            }

            // Check if this target inherits from a parent
            if (InheritanceMap.TryGetValue(target, out var parent))
            {
                // Try user override for the parent
                if (userOverrides is not null && TryGetUserOverride(userOverrides, parent, out var parentSetting))
                {
                    return (
                        !string.IsNullOrEmpty(parentSetting.AllowPattern) ? parentSetting.AllowPattern : null,
                        !string.IsNullOrEmpty(parentSetting.DisallowPattern) ? parentSetting.DisallowPattern : null,
                        !string.IsNullOrEmpty(parentSetting.AllowDescription) ? parentSetting.AllowDescription : null,
                        !string.IsNullOrEmpty(parentSetting.DisallowDescription) ? parentSetting.DisallowDescription : null);
                }

                // Fall through to built-in default for parent
                if (BuiltInDefaults.TryGetValue(parent, out var parentDefault))
                    return parentDefault;
            }

            // Use built-in default for this target
            if (BuiltInDefaults.TryGetValue(target, out var builtIn))
                return builtIn;

            return (null, null, null, null);
        }

        private static bool TryGetUserOverride(
            Dictionary<string, NamingPatternSetting> overrides,
            NamingTarget target,
            out NamingPatternSetting setting)
        {
            var targetName = target.ToString();
            foreach (var kvp in overrides)
            {
                if (SemanticFacts.IsSameName(kvp.Key, targetName))
                {
                    setting = kvp.Value;
                    return true;
                }
            }

            setting = default!;
            return false;
        }

        private static Regex? CompilePattern(string? pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return null;

            try
            {
                return new Regex(
                    pattern.Trim(),
                    RegexOptions.Compiled | RegexOptions.CultureInvariant,
                    RegexTimeout);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }

    internal sealed class ResolvedPatterns
    {
        public static readonly ResolvedPatterns Empty = new(null, null, null, null, null, null);

        public Regex? AllowRegex { get; }
        public Regex? DisallowRegex { get; }
        public string? AllowPatternString { get; }
        public string? DisallowPatternString { get; }
        public string? AllowDescription { get; }
        public string? DisallowDescription { get; }

        public ResolvedPatterns(
            Regex? allowRegex, Regex? disallowRegex,
            string? allowPatternString, string? disallowPatternString,
            string? allowDescription, string? disallowDescription)
        {
            AllowRegex = allowRegex;
            DisallowRegex = disallowRegex;
            AllowPatternString = allowPatternString;
            DisallowPatternString = disallowPatternString;
            AllowDescription = allowDescription;
            DisallowDescription = disallowDescription;
        }
    }

    internal static class RegexExplainer
    {
        public static string? TryExplain(string? pattern, bool isAllow)
        {
            if (string.IsNullOrEmpty(pattern))
                return null;

            // Try to match known simple patterns and translate to English
            var parts = new List<string>();
            var pos = 0;

            while (pos < pattern.Length)
            {
                if (pattern[pos] == '^')
                {
                    pos++;
                    if (pos < pattern.Length && pattern[pos] == '[')
                    {
                        var charClass = TryParseCharacterClass(pattern, ref pos);
                        if (charClass is null)
                            return null;

                        var verb = isAllow ? "must start with" : "must not start with";
                        parts.Add($"{verb} {charClass}");
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (pattern[pos] == '[')
                {
                    var charClass = TryParseCharacterClass(pattern, ref pos);
                    if (charClass is null)
                        return null;

                    var verb = isAllow ? "must contain" : "must not contain";
                    parts.Add($"{verb} {charClass}");
                }
                else if (pattern[pos] == '$')
                {
                    pos++;
                    // End anchor, skip
                }
                else if (pattern[pos] == '.' || pattern[pos] == '*' || pattern[pos] == '+' || pattern[pos] == '?')
                {
                    // Quantifiers and wildcards: skip silently for simple patterns
                    pos++;
                }
                else
                {
                    // Unrecognized construct, bail out
                    return null;
                }
            }

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        private static string? TryParseCharacterClass(string pattern, ref int pos)
        {
            if (pos >= pattern.Length || pattern[pos] != '[')
                return null;

            var endBracket = pattern.IndexOf(']', pos + 1);
            if (endBracket < 0)
                return null;

            var content = pattern.Substring(pos + 1, endBracket - pos - 1);
            pos = endBracket + 1;

            return DescribeCharacterClass(content);
        }

        private static string? DescribeCharacterClass(string content)
        {
            // Common character class patterns
            return content switch
            {
                "A-Z" => "uppercase letter A-Z",
                "a-z" => "lowercase letter a-z",
                "A-Za-z" or "a-zA-Z" => "letter a-z or A-Z",
                "A-Za-z0-9" or "a-zA-Z0-9" => "letter or digit",
                "0-9" => "digit 0-9",
                _ => TryDescribeCharacterList(content)
            };
        }

        private static string? TryDescribeCharacterList(string content)
        {
            // If content is only literal characters (no ranges), list them
            if (content.Length == 0)
                return null;

            // Check for range patterns (contains '-' not at start/end)
            for (int i = 1; i < content.Length - 1; i++)
            {
                if (content[i] == '-')
                    return null; // Contains a range we don't recognize
            }

            // It's a list of literal characters
            var chars = string.Join(", ", content.ToCharArray().Select(c => c.ToString()));
            return $"any of: {chars}";
        }
    }
}
